namespace ReelGrab.Utils;

public class TempFile : IDisposable
{
    public static async Task<TempFile> CreateFromUrlAsync(string fileUrl)
    {
        byte[] bytes;
        using (HttpClient client = new())
        {
            bytes = await client.GetByteArrayAsync(fileUrl);
        }
        string path = System.IO.Path.GetTempFileName();
        await File.WriteAllBytesAsync(path, bytes);
        return new(path);
    }

    public static async Task<TempFile> CreateFromTorrentMagnetAsync(string torrentMagnet)
    {
        string path = System.IO.Path.GetTempFileName() + ".torrent";
        string output = await Commands.RunAsync("/bin/imdl", $"torrent from-link \"{torrentMagnet}\" --output {path}");
        if(output.Contains("Failed to fetch infodict from accessible peers"))
        {
            throw new Exception(output);
        }
        return new(path);
    }

    private TempFile(string path)
    {
        Path = path;
    }

    public string Path { get; init; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}