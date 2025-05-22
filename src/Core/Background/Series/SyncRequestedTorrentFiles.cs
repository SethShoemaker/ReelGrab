
using ReelGrab.Database;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Series;

public class SyncRequestedTorrentFiles : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(30);

    public async Task<List<int>> GetNeededTorrentFileIdsAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("SeriesOutputFile")
            .Join("SeriesTorrentMapping", j => j.On("SeriesOutputFile.SeriesTorrentMappingId", "SeriesTorrentMapping.Id"))
            .Distinct()
            .Select(["SeriesTorrentMapping.TorrentFileId"])
            .GroupBy(["SeriesTorrentMapping.TorrentFileId"])
            .HavingRaw("SUM(CASE WHEN SeriesOutputFile.Status = 'StalePendingUpdate' OR SeriesOutputFile.Status = 'InitializedPendingCreation' THEN 1 ELSE 0 END) > 0")
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
            .Where("Requester", "SeriesComponent")
            .WhereNotIn("TorrentFileId", torrentFileIds)
            .DeleteAsync(cancellationToken: stoppingToken);
        List<int> currentRequestedTorrentFileIds = (await db
            .Query("TorrentFileRequest")
            .Where("Requester", "SeriesComponent")
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
                            "SeriesComponent"
                        })
                );
        }
        transaction.Commit();
    }
}