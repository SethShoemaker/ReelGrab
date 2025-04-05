namespace ReelGrab.Storage.Locations;

public interface IStorageLocation
{
    public string DisplayName { get; }

    public string DisplayType { get; }

    public string Id { get; }

    public Task SaveMovieAsync(int movieId, string type, string fileExtenstion, Stream contents);

    public Task<bool> HasMovieSavedAsync(int movieId, string type);
}