using ReelGrab.Storage.Locations;
using ReelGrab.Utils;

namespace ReelGrab.Configuration;

public class StorageGateway
{
    private StorageGateway(){}

    public static readonly StorageGateway instance = new();

    public readonly string StorageConfigFilePath = "/data/config/storage.json";

    public async Task Apply()
    {
        List<string> localDirectories = await GetLocalDirectories();
        localDirectories
            .Where(path => !Storage.StorageGateway.instance.HasLocalDiskStorageLocation(path))
            .ToList()
            .ForEach(Storage.StorageGateway.instance.AddLocalDiskStorageLocation);
        Storage.StorageGateway.instance.StorageLocations
            .Where(sl => sl.GetType() == typeof(LocalDiskStorageLocation))
            .Select(sl => (sl as LocalDiskStorageLocation)!)
            .Where(sl => !localDirectories.Any(ld => ld == sl.BasePath))
            .Select(sl => sl.BasePath)
            .ToList()
            .ForEach(Storage.StorageGateway.instance.RemoveLocalDiskStorageLocation);
    }

    public Task<List<string>> GetLocalDirectories()
    {
        return Filesystem.GetConfigKeyListString(StorageConfigFilePath, "local_directories");
    }

    public Task SetLocalDirectories(List<string> localDirectories)
    {
        return Filesystem.SetConfigKeyListString(StorageConfigFilePath, "local_directories", localDirectories);
    }
}