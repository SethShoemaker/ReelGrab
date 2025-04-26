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

    private async Task<string> GetFileNameForSeriesEpisodeAsync(int episodeId)
    {
        Application.SeriesEpisodeDetails details = await Application.instance.GetSeriesEpisodeDetailsAsync(episodeId);
        SlugHelperConfiguration config = new();
        config.ForceLowerCase = true;
        config.CollapseWhiteSpace = true;
        config.CollapseDashes = true;
        SlugHelper slug = new();
        string seriesPortion = $"{slug.GenerateSlug(details.SeriesName).Replace('-', '_')}_____{details.SeriesImdbId}";
        return $"{seriesPortion}/{seriesPortion}_____S{SeriesFormatting.FormatSeason(details.SeasonNumber)}E{SeriesFormatting.FormatEpisode(details.EpisodeNumber)}_____{slug.GenerateSlug(details.EpisodeName).Replace('-', '_')}_____{details.EpisodeImdbId}_____{details.SeasonNumber}_{details.EpisodeNumber}";
    }

    public async Task SaveSeriesEpisodeAsync(int episodeId, string type, string fileExtension, Stream contents)
    {
        string path = Path.Join(BasePath, $"{await GetFileNameForSeriesEpisodeAsync(episodeId)}{fileExtension}");
        await Filesystem.WriteStreamToFileAsync(contents, path);
    }

    public async Task<bool> HasSeriesEpisodeAsync(int episodeId, string type)
    {
        Application.SeriesEpisodeDetails details = await Application.instance.GetSeriesEpisodeDetailsAsync(episodeId);
        SlugHelperConfiguration config = new();
        config.ForceLowerCase = true;
        config.CollapseWhiteSpace = true;
        config.CollapseDashes = true;
        SlugHelper slug = new();
        string seriesPortion = $"{slug.GenerateSlug(details.SeriesName).Replace('-', '_')}_____{details.SeriesImdbId}";
        if(!Directory.Exists(Path.Join(BasePath, seriesPortion)))
        {
            return false;
        }
        FileInfo[] files = new DirectoryInfo(Path.Join(BasePath, seriesPortion)).GetFiles("*", SearchOption.TopDirectoryOnly);
        bool exists = files.Any(f => Path.GetFileNameWithoutExtension(f.Name).EndsWith($"{details.SeasonNumber}_{details.EpisodeNumber}"));
        return exists;
    }

    public Task<bool> HasFileByPathAsync(string path)
    {
        return Task.FromResult(File.Exists(Path.Join(BasePath,path)));
    }

    public Task SaveFileByPathAsync(string path, Stream contents)
    {
        return Filesystem.WriteStreamToFileAsync(contents, Path.Join(BasePath,path));
    }
}