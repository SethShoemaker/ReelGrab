namespace ReelGrab.Storage.Locations;

public class LocalDiskStorageLocation : IStorageLocation
{
    public readonly string BasePath;

    public string DisplayName => BasePath;

    public string DisplayType => "LocalDisk";

    public string Id => $"LocalDisk({BasePath})";

    internal LocalDiskStorageLocation(string basePath){
        BasePath = basePath;
    }
}