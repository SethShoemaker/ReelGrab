namespace ReelGrab.TorrentClients;

public class Transmission : ITorrentClient
{    
    public static async Task<Transmission> CreateAsync(string host, int port)
    {
        string output = await Utils.Commands.RunAsync("transmission-remote", $"{host}:{port} -l");
        if(output.Contains("Couldn't connect to server"))
        {
            throw new Exception($"Error connecting to transmission server {host}:{port}");
        }
        return new(host, port);
    }

    private Transmission(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public readonly string Host;

    public readonly int Port;

    public string Name => $"Transmission {Host}:{Port}";

    public async Task ProvisionTorrentByUrlAsync(string torrentFileUrl)
    {
        await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} --start-paused -a {torrentFileUrl}");
    }

    public async Task<List<ITorrentClient.TorrentFileInfo>> GetTorrentFilesByHashAsync(string torrentHash)
    {
        try {
            string output = await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -f");
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
        } catch(Exception e){
            Console.WriteLine(e.StackTrace);
            throw;
        }
    }

    public async Task<bool> HasTorrentByHashAsync(string torrentHash)
    {
        string output = await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -i");
        return output.Length != 0;
    }

    public async Task SetAllTorrentFilesAsNotWantedByHashAsync(string torrentHash)
    {
        var fileNumbers = (await GetTorrentFilesByHashAsync(torrentHash)).Select(tf => tf.Number);
        await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -G {string.Join(',', fileNumbers)}");
    }

    public async Task SetTorrentFilesAsNotWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -G {string.Join(',', fileNumbers)}");
    }

    public async Task SetTorrentFilesAsWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -g {string.Join(',', fileNumbers)}");
    }

    public async Task StartTorrentByHashAsync(string torrentHash)
    {
        await Utils.Commands.RunAsync("transmission-remote", $"{Host}:{Port} -t {torrentHash} -s");
    }
}