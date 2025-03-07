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
        Application.WantedMediaDownloadableType type = await Application.instance.GetWantedMediaDownloadableTypeAsync(downloadableId);
        Application.WantedMediaDownloadableMetadata metadata = await Application.instance.GetWantedMediaDownloadableMetadataAsync(downloadableId);
        SlugHelper slugger = new();
        if(type == Application.WantedMediaDownloadableType.SERIES_EPISODE)
        {
            return $"{slugger.GenerateSlug(metadata.MediaDisplayName)}-{slugger.GenerateSlug(metadata.MediaId)}/{slugger.GenerateSlug(metadata.MediaDisplayName)}-S{SeriesFormatting.FormatSeason(metadata.DownloadableSeason)}E{SeriesFormatting.FormatEpisode(metadata.DownloadableEpisode)}-{slugger.GenerateSlug(metadata.DownloadableDisplayName)}-{slugger.GenerateSlug(downloadableId)}.{fileExtension}";
        }
        if(type == Application.WantedMediaDownloadableType.FULL_MOVIE)
        {
            return $"{slugger.GenerateSlug(metadata.MediaDisplayName)}-{slugger.GenerateSlug(metadata.MediaId)}/{slugger.GenerateSlug(metadata.MediaDisplayName)}-{slugger.GenerateSlug(metadata.MediaId)}.{fileExtension}";
        }
        throw new Exception($"unhandled wanted media downloadable type {type}");
    }
}