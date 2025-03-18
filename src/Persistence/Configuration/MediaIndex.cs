using ReelGrab.Utils;

namespace ReelGrab.Persistence.Configuration;

public class MediaIndex
{
    private MediaIndex() { }

    public static readonly MediaIndex instance = new();

    public readonly string MediaIndexConfigFilePath = "/data/config/media.json";

    public Task<string?> GetOmdbApiKey()
    {
        return Filesystem.GetConfigKeyString(MediaIndexConfigFilePath, "omdb_api_key");
    }

    public Task SetOmdbApiKey(string? omdbApiKey)
    {
        return Filesystem.SetConfigKeyString(MediaIndexConfigFilePath, "omdb_api_key", omdbApiKey);
    }
}