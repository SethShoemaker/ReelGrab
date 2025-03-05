using System.Text.RegularExpressions;
using ReelGrab.TorrentDownloaders;

namespace ReelGrab.Utils;

public static partial class Torrents
{
    public record TorrentFile(string Path, long Bytes);

    public static async Task<List<TorrentFile>> GetTorrentFilesByMagnetAsync(string magnet)
    {
        string filePath = await TorrentDownloader.GetTorrentFilePathByMagnetAsync(magnet);
        return GetTorrentFilesFromShowOutput(await Commands.RunAsync("transmission-show", $"\"{filePath}\""));
    }

    public static async Task<List<TorrentFile>> GetTorrentFilesByUrlAsync(string torrentUrl)
    {
        string filePath = await TorrentDownloader.GetTorrentFilePathByMagnetAsync(torrentUrl);
        return GetTorrentFilesFromShowOutput(await Commands.RunAsync("transmission-show", $"\"{filePath}\""));
    }

    private static List<TorrentFile> GetTorrentFilesFromShowOutput(string showOutput)
    {
        int beg = showOutput.IndexOf("FILES\n");
        if (beg == -1)
        {
            throw new Exception(showOutput);
        }
        beg += 7;
        List<TorrentFile> res = new();
        foreach (string line in showOutput[beg..].Split('\n').Select(l => l.Trim()))
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
        string filePath = await TorrentDownloader.GetTorrentFilePathByUrlAsync(torrentUrl);
        return GetTorrentHashByShowOutput(await Commands.RunAsync("transmission-show", $"\"{filePath}\""));
    }

    private static string GetTorrentHashByShowOutput(string showOutput)
    {
        int beg = showOutput.IndexOf("Hash: ");
        if(beg == -1)
        {
            throw new Exception($"error getting hash for torrent");
        }
        beg += 6;
        int end = showOutput.IndexOf('\n', beg);
        if(end == -1)
        {
            throw new Exception($"error getting hash for torrent");
        }
        return showOutput[beg..end];
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
}
