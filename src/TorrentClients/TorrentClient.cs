using ReelGrab.TorrentClients.Exceptions;
using ReelGrab.TorrentClients.Implementations;

namespace ReelGrab.TorrentClients;

public class TorrentClient : ITorrentClient
{
    public static readonly TorrentClient instance = new();

    private TorrentClient() { }

    public ITorrentClientImplementation? Implementation { get; private set; } = new LocalTransmission();

    public bool Implemented => Implementation != null;

    public Task<bool> ConnectionGoodAsync()
    {
        EnsureTorrentClientExists();
        return Implementation!.ConnectionGoodAsync();
    }

    public Task<Stream> GetCompletedTorrentFileContentsByHashAndFileNumberAsync(string torrentHash, string torrentFilePath)
    {
        EnsureTorrentClientExists();
        return Implementation!.GetCompletedTorrentFileContentsByHashAndFileNumberAsync(torrentHash, torrentFilePath);
    }

    public Task<List<ITorrentClient.TorrentFileInfo>> GetTorrentFilesByHashAsync(string torrentHash)
    {
        EnsureTorrentClientExists();
        return Implementation!.GetTorrentFilesByHashAsync(torrentHash);
    }

    public Task<bool> HasTorrentByHashAsync(string torrentHash)
    {
        EnsureTorrentClientExists();
        return Implementation!.HasTorrentByHashAsync(torrentHash);
    }

    public Task ProvisionTorrentByLocalPathAsync(string torrentLocalPath)
    {
        EnsureTorrentClientExists();
        return Implementation!.ProvisionTorrentByLocalPathAsync(torrentLocalPath);
    }

    public Task ProvisionTorrentByUrlAsync(string torrentFileUrl)
    {
        EnsureTorrentClientExists();
        return Implementation!.ProvisionTorrentByUrlAsync(torrentFileUrl);
    }

    public Task ProvisionTorrentByMagnetAsync(string torrentMagnet)
    {
        EnsureTorrentClientExists();
        return Implementation!.ProvisionTorrentByMagnetAsync(torrentMagnet);
    }

    public Task RemoveTorrentByHashAsync(string torrentHash)
    {
        EnsureTorrentClientExists();
        return Implementation!.RemoveTorrentByHashAsync(torrentHash);
    }

    public Task SetAllTorrentFilesAsNotWantedByHashAsync(string torrentHash)
    {
        EnsureTorrentClientExists();
        return Implementation!.SetAllTorrentFilesAsNotWantedByHashAsync(torrentHash);
    }

    public Task SetTorrentFilesAsNotWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        EnsureTorrentClientExists();
        return Implementation!.SetTorrentFilesAsNotWantedByHashAsync(torrentHash, fileNumbers);
    }

    public Task SetTorrentFilesAsWantedByHashAsync(string torrentHash, List<int> fileNumbers)
    {
        EnsureTorrentClientExists();
        return Implementation!.SetTorrentFilesAsWantedByHashAsync(torrentHash, fileNumbers);
    }

    public Task StartTorrentByHashAsync(string torrentHash)
    {
        EnsureTorrentClientExists();
        return Implementation!.StartTorrentByHashAsync(torrentHash);
    }

    private void EnsureTorrentClientExists()
    {
        if (Implementation == null)
        {
            throw new TorrentClientNotImplementedException();
        }
    }
}