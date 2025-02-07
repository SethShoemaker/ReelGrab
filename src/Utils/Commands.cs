using System.Diagnostics;

namespace ReelGrab.Utils;

public static class Commands
{
    public static async Task<string> RunAsync(string command, string arguments)
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