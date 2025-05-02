using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class DownloadCompletedTorrentFiles : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(60);

    public record TorrentFile(string Hash, string Path, List<TorrentFileDestination> Destinations);

    public record TorrentFileDestination(int OutputFileId, string StorageLocation, string Path);

    private record GetNeededTorrentFileIdsAsyncMovieTorrentFileRow(long Id, string Path, string Hash);

    public record GetNeededTorrentFileIdsAsyncMovieTorrentFileRowTorrentFileDestinationRow(long Id, string StorageLocation, string FilePath);

    public async Task<List<TorrentFile>> GetNeededTorrentFilesAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        var movieTorrentFileIds = (await db
            .Query("MovieOutputFile")
            .Join("MovieTorrentFile", j => j.On("MovieOutputFile.MovieTorrentFileId", "MovieTorrentFile.Id"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Join("MovieTorrent", j => j.On("MovieTorrentFile.MovieTorrentId", "MovieTorrent.Id"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Distinct()
            .Select(["MovieTorrentFile.Id", "TorrentFile.Path", "Torrent.Hash"])
            .GroupBy(["MovieTorrentFile.Id", "TorrentFile.Path", "Torrent.Hash"])
            .HavingRaw("SUM(CASE WHEN MovieOutputFile.Status = 'StalePendingUpdate' OR MovieOutputFile.Status = 'InitializedPendingCreation' THEN 1 ELSE 0 END) > 0")
            .GetAsync<GetNeededTorrentFileIdsAsyncMovieTorrentFileRow>(cancellationToken: cancellationToken))
            .ToList();
        List<TorrentFile> torrentFiles = [];
        foreach (var movieTorrentFile in movieTorrentFileIds)
        {
            torrentFiles.Add(new(
                Hash: movieTorrentFile.Hash,
                Path: movieTorrentFile.Path,
                Destinations: (await db
                    .Query("MovieOutputFile")
                    .Where("MovieTorrentFileId", movieTorrentFile.Id)
                    .WhereIn("Status", ["StalePendingUpdate", "InitializedPendingCreation"])
                    .Select(["Id", "StorageLocation", "FilePath"])
                    .GetAsync<GetNeededTorrentFileIdsAsyncMovieTorrentFileRowTorrentFileDestinationRow>())
                    .Select(d => new TorrentFileDestination(
                        OutputFileId: (int)d.Id,
                        StorageLocation: d.StorageLocation,
                        Path: d.FilePath
                    ))
                    .ToList()
            ));
        }
        return torrentFiles;
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        TorrentClient torrentClient = TorrentClient.instance;
        if (!torrentClient.Implemented || !await torrentClient.ConnectionGoodAsync())
        {
            return;
        }
        foreach (var torrentFile in await GetNeededTorrentFilesAsync(stoppingToken))
        {
            if (!await torrentClient.HasTorrentByHashAsync(torrentFile.Hash))
            {
                continue;
            }
            ITorrentClient.TorrentFileInfo? fileInfo = (await torrentClient
                .GetTorrentFilesByHashAsync(torrentFile.Hash))
                .FirstOrDefault(tf => tf.Path == torrentFile.Path);
            if (fileInfo == null || fileInfo.Progress < 100)
            {
                continue;
            }
            Stream contents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFilePathAsync(torrentFile.Hash, torrentFile.Path);
            foreach (var destination in torrentFile.Destinations)
            {
                using var db = Db.CreateConnection();
                using var transaction = db.Connection.BeginTransaction();
                IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == destination.StorageLocation);
                if (storage == null)
                {
                    continue;
                }
                await db
                    .Query("MovieOutputFile")
                    .Where("Id", destination.OutputFileId)
                    .UpdateAsync(new { Status = "Okay" }, cancellationToken: stoppingToken);
                contents.Seek(0, SeekOrigin.Begin);
                await storage.SaveFileByPathAsync(destination.Path, contents);
                transaction.Commit();
            }
        }
    }
}