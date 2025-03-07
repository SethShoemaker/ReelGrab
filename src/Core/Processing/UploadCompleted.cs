using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using ReelGrab.TorrentClients.Exceptions;
using SqlKata.Execution;

namespace ReelGrab.Core;

public class UploadCompleted : BackgroundService
{
    private record Row(string Hash, string TorrentFilePath, string StorageLocation, string DownloadableId);

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
            .Select(["WantedMediaTorrent.Hash", "WantedMediaTorrentDownloadable.TorrentFilePath", "WantedMediaStorageLocation.StorageLocation", "WantedMediaTorrentDownloadable.DownloadableId"])
            .GetAsync<Row>();
        List<TorrentFile> torrentFiles = [];
        foreach (var row in rows)
        {
            TorrentFile? torrentFile = torrentFiles.FirstOrDefault(tf => tf.TorrentHash == row.Hash && tf.TorrentPath == row.TorrentFilePath && tf.DownloadableId == row.DownloadableId);
            if (torrentFile == null)
            {
                string fileExtension = row.TorrentFilePath[(row.TorrentFilePath.LastIndexOf('.') + 1)..];
                torrentFile = new(row.Hash, row.TorrentFilePath, row.DownloadableId, fileExtension, []);
                torrentFiles.Add(torrentFile);
            }
            if (torrentFile.StorageLocations.FirstOrDefault(sl => sl == row.StorageLocation) == null)
            {
                torrentFile.StorageLocations.Add(row.StorageLocation);
            }
        }
        return torrentFiles;
    }

    private record TorrentFile(string TorrentHash, string TorrentPath, string DownloadableId, string FileExtension, List<string> StorageLocations);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                var torrentClient = TorrentClient.instance;
                if(!torrentClient.Implemented)
                {
                    await Task.Delay(1000 * 3, stoppingToken);
                    continue;
                }
                if (!await torrentClient.ConnectionGoodAsync())
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
                        torrentFile = (await torrentClient.GetTorrentFilesByHashAsync(file.TorrentHash)).FirstOrDefault(tf => tf.Path == file.TorrentPath);
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
                    List<IStorageLocation> storageLocations = StorageGateway.instance.StorageLocations
                        .Where(sl => file.StorageLocations.Any(s => s == sl.Id))
                        .ToList();
                    List<IStorageLocation> newStorageLocations = new();
                    for (int i = 0; i < storageLocations.Count; i++)
                    {
                        if(await storageLocations[i].HasSavedAsync(file.DownloadableId, file.FileExtension))
                        {
                            continue;
                        }
                        newStorageLocations.Add(storageLocations[i]);
                    }
                    storageLocations = newStorageLocations;
                    if(storageLocations.Count == 0)
                    {
                        continue;
                    }
                    using Stream fileContents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFileNumberAsync(file.TorrentHash, file.TorrentPath);
                    foreach (var storageLocation in storageLocations)
                    {
                        Console.WriteLine($"saving {file.DownloadableId} to {storageLocation.DisplayName}");
                        fileContents.Seek(0, SeekOrigin.Begin);
                        await storageLocation.SaveAsync(file.DownloadableId, file.FileExtension, fileContents);
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
                await Task.Delay(1000 * 30, stoppingToken);
            }
        }
    }
}