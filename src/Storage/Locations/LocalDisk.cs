namespace ReelGrab.Storage.Locations;

public class LocalDiskStorageLocation : IStorageLocation
{
    public readonly string BasePath;

    public string DisplayName => BasePath;

    public string DisplayType => "LocalDisk";

    public string Id => BasePath;

    internal LocalDiskStorageLocation(string basePath){
        BasePath = basePath;
    }

    public Task SaveAsync(string path, Stream contents)
    {
        return Utils.Filesystem.WriteStreamToFileAsync(contents, Path.Join(BasePath, path));
    }

    public Task<bool> HasSavedAsync(string path)
    {
        return Task.FromResult(File.Exists(Path.Join(BasePath, path)));
    }
}