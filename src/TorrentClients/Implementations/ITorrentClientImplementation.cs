namespace ReelGrab.TorrentClients.Implementations;

public interface ITorrentClientImplementation : ITorrentClient
{
    public string DisplayName { get; }
}