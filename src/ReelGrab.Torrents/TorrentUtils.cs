using System.Diagnostics;

namespace ReelGrab.Torrents;

public static class TorrentUtils
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
        return await RunCommandAsync("transmission-show", $"\"{tmpFile.Path}\"");
    }

    private static async Task<string> RunCommandAsync(string command, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return string.IsNullOrEmpty(error) ? output : error;
    }
}
