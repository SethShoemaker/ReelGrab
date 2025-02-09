namespace ReelGrab.Storage;

public interface IStorageLocation
{
    public string DisplayName { get; }

    public string DisplayType { get; }

    public string Id { get; }

    public Task Save(string path, Stream contents);
}