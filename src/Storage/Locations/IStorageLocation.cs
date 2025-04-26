namespace ReelGrab.Storage.Locations;

public interface IStorageLocation
{
    public string DisplayName { get; }

    public string DisplayType { get; }

    public string Id { get; }

    public Task<bool> HasFileByPathAsync(string path);

    public Task SaveFileByPathAsync(string path, Stream contents);

    public Task SaveMovieAsync(int movieId, string type, string fileExtenstion, Stream contents);

    public Task<bool> HasMovieSavedAsync(int movieId, string type);

    public Task SaveSeriesEpisodeAsync(int episodeId, string type, string fileExtension, Stream contents);

    public Task<bool> HasSeriesEpisodeAsync(int episodeId, string type);
}