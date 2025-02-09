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

    public Task Save(string path, Stream contents)
    {
        return Utils.Filesystem.WriteStreamToFileAsync(contents, Path.Join(BasePath, path));
    }
}