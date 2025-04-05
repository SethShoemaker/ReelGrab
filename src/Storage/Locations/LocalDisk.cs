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

    private async Task<string> GetFileNameForMovieAsync(int movieId)
    {
        Application.MovieDetails movieDetails = await Application.instance.GetMovieDetailsAsync(movieId);
        SlugHelperConfiguration config = new();
        config.ForceLowerCase = true;
        config.CollapseWhiteSpace = true;
        config.CollapseDashes = true;
        SlugHelper slug = new();
        return $"{slug.GenerateSlug(movieDetails.Name).Replace('-', '_')}_____{movieDetails.ImdbId}_____{movieId}";
    }

    public async Task SaveMovieAsync(int movieId, string type, string fileExtension, Stream contents)
    {
        string path = Path.Join(BasePath, $"{await GetFileNameForMovieAsync(movieId)}{fileExtension}");
        await Filesystem.WriteStreamToFileAsync(contents, path);
    }

    public Task<bool> HasMovieSavedAsync(int movieId, string type)
    {
        FileInfo[] files = new DirectoryInfo(BasePath).GetFiles("*", SearchOption.TopDirectoryOnly);
        bool exists = files.Any(f => Path.GetFileNameWithoutExtension(f.Name).EndsWith(movieId.ToString()));
        return Task.FromResult(exists);
    }
}