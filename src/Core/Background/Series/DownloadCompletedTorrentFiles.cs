using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Series;

public class DownloadCompletedTorrentFiles : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(10);

    public record TorrentFile(string Hash, string Path, List<Destination> Destinations);

    public record Destination(int OutputFileId, string StorageLocation, string Path);

    public record GetTorrentFilesAsyncTorrentFile(string Hash, string Path, long Id);

    public record GetTorrentFilesAsyncDestination(long Id, string StorageLocation, string FilePath);

    public async Task<List<TorrentFile>> GetTorrentFilesAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        var files = await db
            .Query("SeriesOutputFile")
            .Join("SeriesTorrentMapping", j => j.On("SeriesOutputFile.SeriesTorrentMappingId", "SeriesTorrentMapping.Id"))
            .Join("SeriesTorrent", j => j.On("SeriesTorrentMapping.SeriesTorrentId", "SeriesTorrent.Id"))
            .Join("Torrent", j => j.On("SeriesTorrent.TorrentId", "Torrent.Id"))
            .Join("TorrentFile", j => j.On("SeriesTorrentMapping.TorrentFileId", "TorrentFile.Id"))
            .Select(["Torrent.Hash", "TorrentFile.Path", "SeriesTorrentMapping.Id"])
            .GroupBy(["Torrent.Hash", "TorrentFile.Path", "SeriesTorrentMapping.Id"])
            .HavingRaw("SUM(CASE WHEN SeriesOutputFile.Status = 'StalePendingUpdate' OR SeriesOutputFile.Status = 'InitializedPendingCreation' THEN 1 ELSE 0 END) > 0")
            .GetAsync<GetTorrentFilesAsyncTorrentFile>();
        List<TorrentFile> torrentFiles = [];
        foreach (var file in files)
        {
            torrentFiles.Add(new(
                Hash: file.Hash,
                Path: file.Path,
                (await db
                    .Query("SeriesOutputFile")
                    .Where("SeriesTorrentMappingId", file.Id)
                    .WhereIn("Status", ["InitializedPendingCreation", "StalePendingUpdate"])
                    .Select(["Id", "StorageLocation", "FilePath"])
                    .GetAsync<GetTorrentFilesAsyncDestination>())
                    .Select(d => new Destination(
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
        var torrentFiles = await GetTorrentFilesAsync(stoppingToken);
        foreach (var torrentFile in torrentFiles)
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
                    .Query("SeriesOutputFile")
                    .Where("Id", destination.OutputFileId)
                    .UpdateAsync(new { Status = "Okay" }, cancellationToken: stoppingToken);
                contents.Seek(0, SeekOrigin.Begin);
                await storage.SaveFileByPathAsync(destination.Path, contents);
                transaction.Commit();
            }
        }
    }
}