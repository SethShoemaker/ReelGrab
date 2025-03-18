using ReelGrab.Utils;

namespace ReelGrab.Configuration;

public class MediaIndex
{
    private MediaIndex() { }

    public static readonly MediaIndex instance = new();

    public async Task Apply()
    {
        string? omdbApiKey = await GetOmdbApiKey();
        if(omdbApiKey == null)
        {
            MediaIndexes.MediaIndex.instance.RemoveOmdbDatabase();
        }
        else
        {
            MediaIndexes.MediaIndex.instance.AddOmdbDatabase(omdbApiKey);
        }
    }

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