using ReelGrab.Database;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class SyncRequestedTorrentFiles : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(30);

    public async Task<List<int>> GetNeededTorrentFileIdsAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("MovieOutputFile")
            .Join("MovieTorrentFile", j => j.On("MovieOutputFile.MovieTorrentFileId", "MovieTorrentFile.Id"))
            .Distinct()
            .Select(["MovieTorrentFile.TorrentFileId"])
            .GroupBy(["MovieTorrentFile.TorrentFileId"])
            .HavingRaw("SUM(CASE WHEN MovieOutputFile.Status = 'StalePendingUpdate' OR MovieOutputFile.Status = 'InitializedPendingCreation' THEN 1 ELSE 0 END) > 0")
            .GetAsync<long>(cancellationToken: cancellationToken))
            .Select(r => (int)r)
            .ToList();
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        List<int> torrentFileIds = await GetNeededTorrentFileIdsAsync(stoppingToken);
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("TorrentFileRequest")
            .Where("Requester", "MoviesComponent")
            .WhereNotIn("TorrentFileId", torrentFileIds)
            .DeleteAsync(cancellationToken: stoppingToken);
        List<int> currentRequestedTorrentFileIds = (await db
            .Query("TorrentFileRequest")
            .Where("Requester", "MoviesComponent")
            .Select("TorrentFileId")
            .GetAsync<long>())
            .Select(r => (int)r)
            .ToList();
        if (currentRequestedTorrentFileIds.Count != torrentFileIds.Count)
        {
            await db
                .Query("TorrentFileRequest")
                .InsertAsync(
                    ["TorrentFileId", "Requester"],
                    torrentFileIds
                        .Where(i => currentRequestedTorrentFileIds.All(j => i != j))
                        .Select(i => new object[] {
                            i,
                            "MoviesComponent"
                        })
                );
        }
        transaction.Commit();
    }
}