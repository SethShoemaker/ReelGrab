namespace ReelGrab.Storage.Locations;

public interface IStorageLocation
{
    public string DisplayName { get; }

    public string DisplayType { get; }

    public string Id { get; }

    public Task SaveAsync(string path, Stream contents);

    public Task<bool> HasSavedAsync(string path);
}