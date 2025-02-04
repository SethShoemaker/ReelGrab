using System.Diagnostics;

namespace ReelGrab.TorrentDownloader;

public class Transmission : ITorrentClient
{    
    public static async Task<Transmission> CreateAsync(string host, int port)
    {
        string output = await RunCommandAsync("transmission-remote", $"{host}:{port} -l");
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

    static async Task<string> RunCommandAsync(string command, string arguments)
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