using ReelGrab.Database;
using ReelGrab.Utils;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    private static string TorrentFileDir = "/data/torrents";

    public async Task<bool> TorrentWithUrlExistsAsync(string torrentUrl)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("Torrent").Where("Url", torrentUrl).CountAsync<int>()) > 0;
    }

    public async Task<int> AddTorrentAsync(string torrentUrl, string source)
    {
        if (await TorrentWithUrlExistsAsync(torrentUrl))
        {
            throw new Exception($"{torrentUrl} already exists");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        int id = await db
            .Query("Torrent")
            .InsertGetIdAsync<int>(new
            {
                Url = torrentUrl,
                Hash = "",
                Name = "",
                Source = source
            });
        string path = Path.Join(TorrentFileDir, $"{id}.torrent");
        await Torrents.DownloadTorrentAsync(torrentUrl, path);
        await db
            .Query("Torrent")
            .Where("Id", id)
            .UpdateAsync(new
            {
                Hash = await Torrents.GetTorrentHashByFilePathAsync(path),
                Name = await Torrents.GetTorrentNameByFilePathAsync(path)
            });
        var files = await Torrents.GetTorrentFilesByFilePathAsync(path);
        await db
            .Query("TorrentFile")
            .InsertAsync(
                ["TorrentId", "Path", "Bytes"],
                files.Select(f => new object[]{
                    id,
                    f.Path,
                    f.Bytes
                })
            );
        transaction.Commit();
        return id;
    }

    public async Task<int> GetTorrentIdByUrlAsync(string torrentUrl)
    {
        using var db = Db.CreateConnection();
        return await db
            .Query("Torrent")
            .Where("Url", torrentUrl)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
    }

    public async Task<int> GetTorrentFileIdByTorrentIdAndPathAsync(int torrentId, string path)
    {
        using var db = Db.CreateConnection();
        return await db
            .Query("TorrentFile")
            .Where("TorrentId", torrentId)
            .Where("Path", path)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
    }

    public record InspectTorrentWithUrlAsyncResponse(int Id, string Source, List<InspectTorrentWithUrlAsyncFile> Files);

    public record InspectTorrentWithUrlAsyncFile(int Id, string Path, int Bytes);

    private record InspectTorrentWithUrlAsyncRow(long TorrentId, string TorrentSource, long TorrentFileId, string TorrentFilePath, long TorrentFileBytes);

    public async Task<InspectTorrentWithUrlAsyncResponse> InspectTorrentWithUrlAsync(string torrentUrl)
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Torrent")
            .Where("Torrent.Url", torrentUrl)
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .Select(["Torrent.Id AS TorrentId", "Torrent.Source AS TorrentSource", "TorrentFile.Id AS TorrentFileId", "TorrentFile.Path AS TorrentFilePath", "TorrentFile.Bytes AS TorrentFileBytes"])
            .GetAsync<InspectTorrentWithUrlAsyncRow>();
        return new(
            (int)rows.First().TorrentId,
            rows.First().TorrentSource,
            rows.Select(r => new InspectTorrentWithUrlAsyncFile((int)r.TorrentFileId, r.TorrentFilePath, (int)r.TorrentFileBytes)).ToList()
        );
    }
}