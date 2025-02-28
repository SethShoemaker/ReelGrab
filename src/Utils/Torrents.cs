using System.Text.RegularExpressions;

namespace ReelGrab.Utils;

public static partial class Torrents
{
    public record TorrentFile(string Path, long Bytes);

    public static async Task<List<TorrentFile>> GetTorrentFilesByMagnetAsync(string magnet)
    {
        var tmpFile = await TempFile.CreateFromTorrentMagnetAsync(magnet);
        string show = await Commands.RunAsync("transmission-show", $"\"{tmpFile.Path}\"");
        int beg = show.IndexOf("FILES\n");
        if (beg == -1)
        {
            throw new Exception(show);
        }
        beg += 7;
        List<TorrentFile> res = new();
        foreach (string line in show[beg..].Split('\n').Select(l => l.Trim()))
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            int sizeBeg = line.LastIndexOf('(');
            int sizeEnd = line.LastIndexOf(')');
            int unitBeg = line.LastIndexOf(' ') + 1;
            float size = float.Parse(line[(sizeBeg + 1)..unitBeg]);
            string unit = line[unitBeg..sizeEnd];
            string path = line[..(sizeBeg - 1)];
            res.Add(new(path, ConvertToBytes(size, unit)));
        }
        return res;
    }

    public static async Task<List<TorrentFile>> GetTorrentFilesByUrlAsync(string torrentUrl)
    {
        string show = await RunTorrentShowAsync(torrentUrl);
        int beg = show.IndexOf("FILES\n");
        if (beg == -1)
        {
            throw new Exception();
        }
        beg += 7;
        List<TorrentFile> res = new();
        foreach (string line in show[beg..].Split('\n').Select(l => l.Trim()))
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            int sizeBeg = line.LastIndexOf('(');
            int sizeEnd = line.LastIndexOf(')');
            int unitBeg = line.LastIndexOf(' ') + 1;
            float size = float.Parse(line[(sizeBeg + 1)..unitBeg]);
            string unit = line[unitBeg..sizeEnd];
            string path = line[..(sizeBeg - 1)];
            res.Add(new(path, ConvertToBytes(size, unit)));
        }
        return res;
    }

    private static long ConvertToBytes(float size, string unit)
    {
        Dictionary<string, long> unitMultipliers = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            { "B", 1L },
            { "KB", 1_024L },
            { "MB", 1_024L * 1_024L },
            { "GB", 1_024L * 1_024L * 1_024L },
            { "TB", 1_024L * 1_024L * 1_024L * 1_024L },
            { "PB", 1_024L * 1_024L * 1_024L * 1_024L * 1_024L }
        };

        if (!unitMultipliers.TryGetValue(unit.ToUpper(), out long multiplier))
        {
            throw new ArgumentException("Invalid unit", nameof(unit));
        }

        return (long)size * multiplier;
    }

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
