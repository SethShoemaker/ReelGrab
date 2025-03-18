using ReelGrab.Utils;

namespace ReelGrab.Persistence.Configuration;

public class StorageGateway
{
    private StorageGateway(){}

    public static readonly StorageGateway instance = new();

    public readonly string StorageConfigFilePath = "/data/config/storage.json";

    public Task<List<string>> GetLocalDirectories()
    {
        return Filesystem.GetConfigKeyListString(StorageConfigFilePath, "local_directories");
    }

    public Task SetLocalDirectories(List<string> localDirectories)
    {
        return Filesystem.SetConfigKeyListString(StorageConfigFilePath, "local_directories", localDirectories);
    }
}