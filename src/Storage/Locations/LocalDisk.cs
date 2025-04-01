using ReelGrab.Core;
using ReelGrab.Utils;
using Slugify;

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

    public async Task SaveAsync(string downloadableId, string fileExtension, Stream contents)
    {
        string path = Path.Join(BasePath, await GetFilePathForDownloadable(downloadableId, fileExtension));
        await Filesystem.WriteStreamToFileAsync(contents, path);
    }

    public async Task<bool> HasSavedAsync(string downloadableId, string fileExtension)
    {
        string path = Path.Join(BasePath, await GetFilePathForDownloadable(downloadableId, fileExtension));
        return File.Exists(path);
    }

    private async Task<string> GetFilePathForDownloadable(string downloadableId, string fileExtension)
    {
        throw new NotImplementedException();
    }
}