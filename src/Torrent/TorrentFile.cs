using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public class TorrentFile
{
    public string InfoHash { get; init; } = null!;

    public async static Task<TorrentFile> FromFileAsync(string filePath)
    {
        var doc = await Document.FromFileAsync(filePath);
        return new TorrentFile()
        {
            InfoHash = TorrentUtils.GetInfoHash(doc)
        };
    }
}
