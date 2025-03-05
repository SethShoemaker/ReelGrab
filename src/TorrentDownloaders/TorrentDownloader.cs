using ReelGrab.Database;
using ReelGrab.Utils;
using SqlKata.Execution;

namespace ReelGrab.TorrentDownloaders;

public class TorrentDownloader
{
    private static string TorrentFileDir = "/app/torrents";

    private record DownloadedTorrentRow(string FileGuid);

    public static async Task<string> GetTorrentFilePathByUrlAsync(string torrentUrl)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("DownloadedTorrent")
            .Where("Url", torrentUrl)
            .Select("FileGuid")
            .FirstOrDefaultAsync<DownloadedTorrentRow>();
        if (row != null)
        {
            return Path.Join(TorrentFileDir, $"{row.FileGuid}.torrent");
        }
        Guid fileGuid = Guid.NewGuid();
        string path = Path.Join(TorrentFileDir, $"{fileGuid}.torrent");
        byte[] bytes;
        using (HttpClient client = new())
        {
            bytes = await client.GetByteArrayAsync(torrentUrl);
        }
        await File.WriteAllBytesAsync(path, bytes);
        await db
            .Query("DownloadedTorrent")
            .InsertAsync(new { Url = torrentUrl, FileGuid = fileGuid.ToString() });
        return path;
    }

    public static async Task<string> GetTorrentFilePathByMagnetAsync(string torrentMagnet)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("DownloadedTorrent")
            .Where("Url", torrentMagnet)
            .Select("FileGuid")
            .FirstOrDefaultAsync<DownloadedTorrentRow>();
        if (row != null)
        {
            return Path.Join(TorrentFileDir, $"{row.FileGuid}.torrent");
        }
        Guid fileGuid = Guid.NewGuid();
        string path = Path.Join(TorrentFileDir, $"{fileGuid}.torrent");
        string output = await Commands.RunAsync("/root/bin/imdl", $"torrent from-link \"{torrentMagnet}\" --output {path}");
        if (output.Contains("Failed to fetch infodict from accessible peers"))
        {
            throw new Exception(output);
        }
        await db
           .Query("DownloadedTorrent")
           .InsertAsync(new { Url = torrentMagnet, FileGuid = fileGuid.ToString() });
        return path;
    }
}