using ReelGrab.Database;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Processing;

public class SyncTorrentFiles : BackgroundService
{
    private record Row(string TorrentFilePath, string TorrentUrl, string Hash);

    private async Task<List<TorrentFile>> GetAllTorrentFilesAsync()
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("WantedMediaTorrentDownloadable")
            .Join("WantedMediaTorrent", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaTorrent.MediaId")
                .On("WantedMediaTorrentDownloadable.TorrentDisplayName", "WantedMediaTorrent.DisplayName"))
            .Select(["TorrentFilePath", "TorrentUrl", "Hash"])
            .GetAsync<Row>();
        List<TorrentFile> torrentFiles = new();
        foreach (var row in rows)
        {
            TorrentFile? torrentFile = torrentFiles.FirstOrDefault(tf => tf.Url == row.TorrentUrl);
            if (torrentFile == null)
            {
                torrentFile = new(row.TorrentUrl, row.Hash, new());
                torrentFiles.Add(torrentFile);
            }
            torrentFile.FilePaths.Add(row.TorrentFilePath);
        }
        return torrentFiles;
    }

    private record TorrentFile(string Url, string Hash, List<string> FilePaths);

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                ITorrentClient? torrentClient = TorrentClientConfig.instance.torrentClient;
                if (torrentClient == null)
                {
                    await Task.Delay(1000 * 3, stoppingToken);
                    continue;
                }
                var files = await GetAllTorrentFilesAsync();
                foreach (var file in files)
                {
                    if (!await torrentClient.HasTorrentByHashAsync(file.Hash))
                    {
                        await torrentClient.ProvisionTorrentByUrlAsync(file.Url);
                        await torrentClient.SetAllTorrentFilesAsNotWantedByHashAsync(file.Hash);
                    }
                    List<ITorrentClient.TorrentFileInfo> torrentFiles = await torrentClient.GetTorrentFilesByHashAsync(file.Hash);
                    var torrentFilesToNowWant = torrentFiles
                        .Where(f => !f.Wanted && file.FilePaths.Any(p => p == f.Path))
                        .Select(f => f.Number)
                        .ToList();
                    await torrentClient.SetTorrentFilesAsWantedByHashAsync(file.Hash, torrentFilesToNowWant);
                    var torrentFilesToNowNotWant = torrentFiles
                        .Where(f => f.Wanted && !file.FilePaths.Any(p => p == f.Path))
                        .Select(f => f.Number)
                        .ToList();
                    await torrentClient.SetTorrentFilesAsNotWantedByHashAsync(file.Hash, torrentFilesToNowNotWant);
                    await torrentClient.StartTorrentByHashAsync(file.Hash);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
            }
            finally
            {
                await Task.Delay(1000 * 3, stoppingToken);
            }
        }
    }
}