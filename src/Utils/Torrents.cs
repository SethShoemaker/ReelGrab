using System.Text.RegularExpressions;

namespace ReelGrab.Utils;

public static partial class Torrents
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

    [GeneratedRegex(@"xt=urn:btih:([a-fA-F0-9]{40}|[A-Z2-7]{32})")]
    private static partial Regex MagnetInfoHash();

    public static Task<string> GetTorrentHashByMagnetLinkAsync(string magnetLink)
    {
        var match = MagnetInfoHash().Match(magnetLink);
        if(!match.Success)
        {
            throw new Exception($"{magnetLink} is not a valid magnet link");
        }
        return Task.FromResult(match.Groups[1].Value);
    }

    private static async Task<string> RunTorrentShowAsync(string torrentUrl)
    {
        using var tmpFile = await TempFile.CreateFromUrlAsync(torrentUrl);
        return await Commands.RunAsync("transmission-show", $"\"{tmpFile.Path}\"");
    }
}
