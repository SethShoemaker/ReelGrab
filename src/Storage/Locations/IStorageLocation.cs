namespace ReelGrab.Storage.Locations;

public interface IStorageLocation
{
    public string DisplayName { get; }

    public string DisplayType { get; }

    public string Id { get; }

    public Task SaveAsync(string downloadableId, string fileExtension, Stream contents);

    public Task<bool> HasSavedAsync(string downloadableId, string fileExtension);
}