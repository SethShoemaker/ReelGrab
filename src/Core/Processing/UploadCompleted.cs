using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.TorrentClients;
using ReelGrab.TorrentClients.Exceptions;
using SqlKata.Execution;

namespace ReelGrab.Core;

public class UploadCompleted : BackgroundService
{
    private record Row(string Hash, string TorrentFilePath, string StorageLocation);

    private async Task<IEnumerable<TorrentFile>> GetTorrentFiles()
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("WantedMediaTorrentDownloadable")
            .Join("WantedMediaTorrent", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaTorrent.MediaId")
                .On("WantedMediaTorrentDownloadable.TorrentDisplayName", "WantedMediaTorrent.DisplayName"))
            .Join("WantedMediaStorageLocation", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaStorageLocation.MediaId"))
            .Select(["WantedMediaTorrent.Hash", "WantedMediaTorrentDownloadable.TorrentFilePath", "WantedMediaStorageLocation.StorageLocation"])
            .GetAsync<Row>();
        List<TorrentFile> torrentFiles = [];
        foreach (var row in rows)
        {
            TorrentFile? torrentFile = torrentFiles.FirstOrDefault(tf => tf.Hash == row.Hash && tf.Path == row.TorrentFilePath);
            if (torrentFile == null)
            {
                torrentFile = new(row.Hash, row.TorrentFilePath, []);
                torrentFiles.Add(torrentFile);
            }
            if (torrentFile.StorageLocations.FirstOrDefault(sl => sl == row.StorageLocation) == null)
            {
                torrentFile.StorageLocations.Add(row.StorageLocation);
            }
        }
        return torrentFiles;
    }

    private record TorrentFile(string Hash, string Path, List<string> StorageLocations);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                ITorrentClient? torrentClient = TorrentClientConfig.instance.torrentClient;
                if (torrentClient == null)
                {
                    await Task.Delay(1000 * 3, stoppingToken);
                    continue;
                }
                var files = await GetTorrentFiles();
                foreach (var file in files)
                {
                    ITorrentClient.TorrentFileInfo? torrentFile;
                    try
                    {
                        torrentFile = (await torrentClient.GetTorrentFilesByHashAsync(file.Hash)).FirstOrDefault(tf => tf.Path == file.Path);
                    }
                    catch (TorrentDoesNotExistException)
                    {
                        continue;
                    }
                    if (torrentFile == null)
                    {
                        continue;
                    }
                    if (torrentFile.Progress != 100)
                    {
                        continue;
                    }
                    using Stream fileContents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFileNumberAsync(file.Hash, file.Path);
                    IEnumerable<IStorageLocation> storageLocations = StorageGateway.instance.StorageLocations.Where(sl => file.StorageLocations.Any(s => s == sl.Id));
                    foreach (var storageLocation in storageLocations)
                    {
                        fileContents.Seek(0, SeekOrigin.Begin);
                        await storageLocation.Save(file.Path, fileContents);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
            }
            finally
            {
                await Task.Delay(1000 * 5);
            }
        }
    }
}