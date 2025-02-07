namespace ReelGrab.Utils;

public static class Torrents
{
    public static async Task<string> GetTorrentHashByUrlAsync(string torrentUrl)
    {
        string show = await RunTorrentShowAsync(torrentUrl);
        int beg = show.IndexOf("Hash: ");
        if(beg == -1)
        {
            throw new Exception($"error getting hash for torrent {torrentUrl}");
        }
        beg += 6;
        int end = show.IndexOf('\n', beg);
        if(end == -1)
        {
            throw new Exception($"error getting hash for torrent {torrentUrl}");
        }
        return show[beg..end];
    }

    private static async Task<string> RunTorrentShowAsync(string torrentUrl)
    {
        using var tmpFile = await TempFile.CreateFromUrlAsync(torrentUrl);
        return await Commands.RunAsync("transmission-show", $"\"{tmpFile.Path}\"");
    }
}
