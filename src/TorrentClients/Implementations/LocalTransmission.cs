using ReelGrab.TorrentClients.Exceptions;
using ReelGrab.Utils;

namespace ReelGrab.TorrentClients.Implementations;

public class LocalTransmission : ITorrentClientImplementation
{
    public LocalTransmission() { }

    private readonly string baseDownloadPath = "/data/transmission/downloads";

    public readonly string Host = "localhost";

    public readonly int Port = 9091;

    public string DisplayName => $"Local Transmission";

    public async Task<bool> ConnectionGoodAsync()
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -l");
        return output.Contains("Done") && output.Contains("ETA");
    }

    public async Task ProvisionTorrentByLocalPathAsync(string torrentLocalPath)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} --start-paused -a {torrentLocalPath}");
        if (!output.ContainsMoreThanOnce("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task RemoveTorrentByHashAsync(string torrentHash)
    {
        await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} --remove-and-delete");
    }

    public async Task<List<ITorrentClient.TorrentFileInfo>> GetTorrentFilesByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -f");
        if (output.Length == 0)
        {
            throw new TorrentDoesNotExistException(torrentHash);
        }
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[2..];
        List<ITorrentClient.TorrentFileInfo> files = new();
        foreach (var line in lines)
        {
            int number = int.Parse(line[..line.IndexOf(':')]);
            string path = line[34..];
            int progress = int.Parse(string.Join("", line[4..9].Trim().Where(c => c != '%')));
            bool get = line[18..22].Trim() switch
            {
                "Yes" => true,
                "No" => false,
                _ => throw new Exception("error while parsing transmission-remote command output")
            };
            files.Add(new(number, path, progress, get));
        }
        return files;
    }

    public async Task<bool> HasTorrentByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -i");
        return output.Length != 0;
    }

    public async Task SetAllTorrentFilesAsNotWantedByHashAsync(string torrentHash)
    {
        var fileNumbers = (await GetTorrentFilesByHashAsync(torrentHash)).Select(tf => tf.Number);
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -G {string.Join(',', fileNumbers)}");
        if (output.Length == 0)
        {
            throw new TorrentDoesNotExistException(torrentHash);
        }
    }

    public async Task SetTorrentFilesAsNotWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        if (fileNumbers.Count == 0)
        {
            return;
        }
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -G {string.Join(',', fileNumbers)}");
        if (!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task SetTorrentFilesAsWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        if (fileNumbers.Count == 0)
        {
            return;
        }
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -g {string.Join(',', fileNumbers)}");
        if (!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task StartTorrentByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -s");
        if (!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public Task<Stream> GetCompletedTorrentFileContentsByHashAndFilePathAsync(string torrentHash, string torrentFilePath)
    {
        return Task.FromResult((Stream)File.OpenRead(Path.Join(baseDownloadPath, torrentFilePath)));
    }

    public async Task<string> GetTorrentNameByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -i");
        int bef = output.IndexOf("Name: ");
        if (bef == -1)
        {
            throw new TorrentException(output);
        }
        bef += 6;
        int after = output.IndexOf('\n', bef);
        if (after == -1)
        {
            throw new TorrentException(output);
        }
        return output[bef..after];
    }

    private static async Task<string> RunTransmissionCommandAsync(string command)
    {
        string output = await Commands.RunAsync("transmission-remote", command);
        if (output.Contains("Couldn't resolve host name"))
        {
            throw new TorrentException(output);
        }
        if (output.Contains("Couldn't connect to server"))
        {
            throw new TorrentException(output);
        }
        return output;
    }
}