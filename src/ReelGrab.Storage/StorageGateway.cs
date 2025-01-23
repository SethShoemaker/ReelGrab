using System.Collections.ObjectModel;
using ReelGrab.Storage.Locations;

namespace ReelGrab.Storage;

public partial class StorageGateway
{
    private StorageGateway(){}

    public static readonly StorageGateway instance = new();

    private List<IStorageLocation> storageLocations = [];

    public ReadOnlyCollection<IStorageLocation> StorageLocations
    {
        get
        {
            return storageLocations.AsReadOnly();
        }
    }

    public void AddLocalDiskStorageLocation(string basePath)
    {
        if(storageLocations.Where(sl => sl.GetType() == typeof(LocalDiskStorageLocation) && (sl as LocalDiskStorageLocation)!.BasePath == basePath).Count() > 0){
            throw new Exception($"{basePath} is already being used as a storage location");
        }
        storageLocations.Add(new LocalDiskStorageLocation(basePath));
    }

    public void RemoveLocalDiskStorageLocation(string basePath)
    {
        storageLocations = storageLocations.Where(sl => sl.GetType() != typeof(LocalDiskStorageLocation) || (sl as LocalDiskStorageLocation)!.BasePath != basePath).ToList();
    }

    public bool HasLocalDiskStorageLocation(string basePath)
    {
        return storageLocations.Where(sl => sl.GetType() == typeof(LocalDiskStorageLocation) && (sl as LocalDiskStorageLocation)!.BasePath == basePath).Count() > 0;
    }

    public void RemoveAllLocalDiskStorageLocations()
    {
        storageLocations = storageLocations.Where(sl => sl.GetType() != typeof(LocalDiskStorageLocation)).ToList();
    }
}