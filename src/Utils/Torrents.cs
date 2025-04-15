namespace ReelGrab.Utils;

public static partial class Torrents
{
    public record TorrentFile(string Path, long Bytes);

    public static async Task<List<TorrentFile>> GetTorrentFilesByFilePathAsync(string filePath)
    {
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

    public static async Task<string> GetTorrentHashByFilePathAsync(string filePath)
    {
        return GetTorrentHashByShowOutput(await Commands.RunAsync("transmission-show", $"\"{filePath}\""));
    }

    private static string GetTorrentHashByShowOutput(string showOutput)
    {
        int beg = showOutput.IndexOf("Hash: ");
        if (beg == -1)
        {
            throw new Exception($"error getting hash for torrent");
        }
        beg += 6;
        int end = showOutput.IndexOf('\n', beg);
        if (end == -1)
        {
            throw new Exception($"error getting hash for torrent");
        }
        return showOutput[beg..end];
    }

    public static async Task<string> GetTorrentNameByFilePathAsync(string filePath)
    {
        return GetTorrentNameByShowOutput(await Commands.RunAsync("transmission-show", $"\"{filePath}\""));
    }

    private static string GetTorrentNameByShowOutput(string showOutput)
    {
        int beg = showOutput.IndexOf("Name: ");
        if (beg == -1)
        {
            throw new Exception($"error getting name for torrent");
        }
        beg += 6;
        int end = showOutput.IndexOf('\n', beg);
        if (end == -1)
        {
            throw new Exception($"error getting name for torrent");
        }
        return showOutput[beg..end];
    }

    public static Task DownloadTorrentAsync(string torrent, string filePath)
    {
        return torrent.StartsWith("magnet:")
            ? DownloadTorrentByMagnetAsync(torrent, filePath)
            : DownloadTorrentByUrlAsync(torrent, filePath);
    }

    public static async Task DownloadTorrentByMagnetAsync(string magnet, string filePath)
    {
        string output = await Commands.RunAsync("/bin/imdl", $"torrent from-link \"{magnet}\" --output {filePath}");
        if (output.Contains("Failed to fetch infodict from accessible peers"))
        {
            throw new Exception(output);
        }
    }

    public static async Task DownloadTorrentByUrlAsync(string torrentUrl, string filePath)
    {
        byte[] bytes;
        using (HttpClient http = new())
        {
            bytes = await http.GetByteArrayAsync(torrentUrl);
        }
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}
