using ReelGrab.TorrentClients.Exceptions;
using ReelGrab.Utils;

namespace ReelGrab.TorrentClients;

public class Transmission : ITorrentClient
{    
    public static async Task<Transmission> CreateAsync(string host, int port)
    {
        string output = await RunTransmissionCommandAsync($"{host}:{port} -l");
        if(!output.Contains("Done") || !output.Contains("ETA"))
        {
            throw new TorrentException(output);
        }
        return new(host, port);
    }

    private Transmission(string host, int port)
    {
        Host = host;
        Port = port;
    }

    private readonly string baseDownloadPath = "/data/transmission/downloads";

    public readonly string Host;

    public readonly int Port;

    public string Name => $"Transmission {Host}:{Port}";

    public async Task ProvisionTorrentByUrlAsync(string torrentFileUrl)
    {
        using TempFile tmpFile = await TempFile.CreateFromUrlAsync(torrentFileUrl);
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} --start-paused -a {tmpFile.Path}");
        if(!output.ContainsMoreThanOnce("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task<List<ITorrentClient.TorrentFileInfo>> GetTorrentFilesByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -f");
        if(output.Length == 0)
        {
            throw new TorrentDoesNotExistException(torrentHash);
        }
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[2..];
        List<ITorrentClient.TorrentFileInfo> files = new();
        foreach(var line in lines)
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
        if(output.Length == 0)
        {
            throw new TorrentDoesNotExistException(torrentHash);
        }
    }

    public async Task SetTorrentFilesAsNotWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        if(fileNumbers.Count == 0)
        {
            return;
        }
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -G {string.Join(',', fileNumbers)}");
        if(!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task SetTorrentFilesAsWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        if(fileNumbers.Count == 0)
        {
            return;
        }
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -g {string.Join(',', fileNumbers)}");
        if(!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task StartTorrentByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -s");
        if(!output.Contains("responded: \"success\""))
        {
            throw new TorrentException(output);
        }
    }

    public async Task<Stream> GetCompletedTorrentFileContentsByHashAndFileNumberAsync(string torrentHash, string torrentFilePath)
    {
        string name = await GetTorrentNameByHashAsync(torrentHash);
        return File.OpenRead(Path.Join(baseDownloadPath, torrentFilePath));
    }

    public async Task<string> GetTorrentNameByHashAsync(string torrentHash)
    {
        string output = await RunTransmissionCommandAsync($"{Host}:{Port} -t {torrentHash} -i");
        int bef = output.IndexOf("Name: ");
        if(bef == -1)
        {
            throw new TorrentException(output);
        }
        bef += 6;
        int after = output.IndexOf('\n', bef);
        if(after == -1)
        {
            throw new TorrentException(output);
        }
        return output[bef..after];
    }

    private static async Task<string> RunTransmissionCommandAsync(string command)
    {
        string output = await Commands.RunAsync("transmission-remote", command);
        if(output.Contains("Couldn't resolve host name"))
        {
            throw new TorrentException(output);
        }
        if(output.Contains("Couldn't connect to server"))
        {
            throw new TorrentException(output);
        }
        return output;
    }
}