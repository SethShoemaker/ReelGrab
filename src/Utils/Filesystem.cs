namespace ReelGrab.Utils;

public static class Filesystem
{
    public static async Task WriteStreamToFileAsync(Stream inputStream, string filePath)
    {
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await inputStream.CopyToAsync(fileStream);
    }
}