namespace ReelGrab.Storage.Locations;

public class LocalDiskStorageLocation : IStorageLocation
{
    public readonly string BasePath;

    public string DisplayName => BasePath;

    internal LocalDiskStorageLocation(string basePath){
        BasePath = basePath;
    }
}