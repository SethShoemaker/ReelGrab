using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
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
                    List<IStorageLocation> storageLocations = StorageGateway.instance.StorageLocations
                        .Where(sl => file.StorageLocations.Any(s => s == sl.Id))
                        .ToList();
                    List<IStorageLocation> newStorageLocations = new();
                    for (int i = 0; i < storageLocations.Count; i++)
                    {
                        if(await storageLocations[i].HasSavedAsync(file.Path))
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
                    using Stream fileContents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFileNumberAsync(file.Hash, file.Path);
                    foreach (var storageLocation in storageLocations)
                    {
                        Console.WriteLine($"saving {file.Path} to {storageLocation.DisplayName}");
                        fileContents.Seek(0, SeekOrigin.Begin);
                        await storageLocation.SaveAsync(file.Path, fileContents);
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