namespace ReelGrab.TorrentClients;

public interface ITorrentClient
{
    public string Name { get; }

    public Task<bool> HasTorrentByHashAsync(string torrentHash);

    public Task ProvisionTorrentByUrlAsync(string torrentFileUrl);

    public record TorrentFileInfo(int Number, string Path, int Progress, bool Wanted);

    public Task<List<TorrentFileInfo>> GetTorrentFilesByHashAsync(string torrentHash);

    public Task SetAllTorrentFilesAsNotWantedByHashAsync(string torrentHash);

    public Task SetTorrentFilesAsNotWantedByHashAsync(string torrentHash, List<int> fileNumbers);

    public Task SetTorrentFilesAsWantedByHashAsync(string torrentHash, List<int> fileNumbers);

    public Task StartTorrentByHashAsync(string torrentHash);

    public Task<Stream> GetCompletedTorrentFileContentsByHashAndFileNumberAsync(string torrentHash, string torrentFilePath);
}