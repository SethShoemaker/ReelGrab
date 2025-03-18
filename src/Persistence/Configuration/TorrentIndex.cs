using ReelGrab.Utils;

namespace ReelGrab.Persistence.Configuration;

public class TorrentIndex
{
    private TorrentIndex() { }

    public static readonly TorrentIndex instance = new();

    public readonly string TorrentIndexConfigFilePath = "/data/config/jackett.json";

    public Task<string?> GetJackettApiUrl()
    {
        return Filesystem.GetConfigKeyString(TorrentIndexConfigFilePath, "jackett_api_url");
    }

    public Task SetJackettApiUrl(string? jackettApiUrl)
    {
        return Filesystem.SetConfigKeyString(TorrentIndexConfigFilePath, "jackett_api_url", jackettApiUrl);
    }

    public Task<string?> GetJackettApiKey()
    {
        return Filesystem.GetConfigKeyString(TorrentIndexConfigFilePath, "jackett_api_key");
    }

    public Task SetJackettApiKey(string? jackettApiKey)
    {
        return Filesystem.SetConfigKeyString(TorrentIndexConfigFilePath, "jackett_api_key", jackettApiKey);
    }
}