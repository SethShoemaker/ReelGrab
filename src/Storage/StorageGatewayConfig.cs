using ReelGrab.Database;
using ReelGrab.Storage.Locations;
using SqlKata.Execution;

namespace ReelGrab.Storage;

public class StorageGatewayConfig
{
    public static readonly StorageGatewayConfig instance = new();

    private StorageGatewayConfig() { }

    public readonly StorageGateway storageGateway = StorageGateway.instance;

    public record StorageGatewayConfigRow(string Key, string Value);

    public async Task ApplyStorageGatewayConfigAsync()
    {
        using QueryFactory db = Db.CreateConnection();
        var configs = await db.Query("StorageGatewayConfig").Select("Key", "Value").GetAsync<StorageGatewayConfigRow>();

        void applyLocalDisksIfAble(IEnumerable<StorageGatewayConfigRow> rows)
        {
            var row = rows.FirstOrDefault(c => c.Key == StorageGatewayConfigKey.LOCAL_DISK_LOCATIONS.ToString());
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                storageGateway.RemoveAllLocalDiskStorageLocations();
            }
            else
            {
                string[] newPaths = row.Value.Split(",");
                newPaths
                    .Where(path => !storageGateway.HasLocalDiskStorageLocation(path))
                    .ToList()
                    .ForEach(storageGateway.AddLocalDiskStorageLocation);
                storageGateway.StorageLocations
                    .Where(sl => sl.GetType() == typeof(LocalDiskStorageLocation))
                    .Select(sl => (sl as LocalDiskStorageLocation)!)
                    .Where(sl => !newPaths.Any(np => np == sl.BasePath))
                    .Select(sl => sl.BasePath)
                    .ToList()
                    .ForEach(storageGateway.RemoveLocalDiskStorageLocation);
            }
        }
        applyLocalDisksIfAble(configs);
    }

    public async Task<Dictionary<StorageGatewayConfigKey, string?>> GetStorageGatewayConfigAsync()
    {
        Dictionary<string, StorageGatewayConfigRow> rowsDict;
        using (var db = Db.CreateConnection())
        {
            rowsDict = (await db.Query("StorageGatewayConfig").Select("Key", "Value").GetAsync<StorageGatewayConfigRow>()).ToDictionary(r => r.Key);
        }
        Dictionary<StorageGatewayConfigKey, string?> result = new();
        foreach (StorageGatewayConfigKey key in Enum.GetValues<StorageGatewayConfigKey>())
        {
            result[key] = rowsDict.ContainsKey(key.ToString()) ? rowsDict[key.ToString()].Value : null;
        }
        return result;
    }

    public async Task SetStorageGatewayConfigAsync(Dictionary<StorageGatewayConfigKey, string?> configs)
    {
        // ensure no empty strings
        foreach (var key in configs.Keys)
        {
            if (configs[key] != null && configs[key]!.Length == 0)
            {
                configs[key] = null;
            }
        }
        using QueryFactory db = Db.CreateConnection();
        foreach (StorageGatewayConfigKey key in configs.Keys)
        {
            int count = (await db.Query("StorageGatewayConfig").Where("Key", key.ToString()).AsCount().GetAsync<int>()).First();
            if (count > 0)
            {
                await db.Query("StorageGatewayConfig").Where("Key", key.ToString()).UpdateAsync(new
                {
                    Value = configs[key]
                });
            }
            else
            {
                await db.Query("StorageGatewayConfig").InsertAsync(new
                {
                    Key = key.ToString(),
                    Value = configs[key]
                });
            }
        }
        await ApplyStorageGatewayConfigAsync();
    }
}