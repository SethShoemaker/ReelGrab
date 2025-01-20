using ReelGrab.Media;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public readonly MediaIndex mediaIndex = MediaIndex.instance;

    public record MediaIndexConfigRow(string Key, string Value);

    public async Task ApplyMediaIndexConfigAsync()
    {
        using QueryFactory db = Db();
        var configs = await db.Query("MediaIndexConfig").Select("Key", "Value").GetAsync<MediaIndexConfigRow>();

        void applyOmdbIfAble(IEnumerable<MediaIndexConfigRow> rows)
        {
            var row = rows.FirstOrDefault(c => c.Key == MediaIndexConfigKey.OMDb_API_Key.ToString());
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                MediaIndex.instance.RemoveOmdbDatabase();
            }
            else
            {
                MediaIndex.instance.AddOmdbDatabase(row.Value);
            }
        }

        applyOmdbIfAble(configs);
    }

    public async Task<Dictionary<MediaIndexConfigKey, string?>> GetMediaIndexConfigAsync()
    {
        Dictionary<string, MediaIndexConfigRow> rowsDict;
        using (var db = Db())
        {
            rowsDict = (await db.Query("MediaIndexConfig").Select("Key", "Value").GetAsync<MediaIndexConfigRow>()).ToDictionary(r => r.Key);
        }
        Dictionary<MediaIndexConfigKey, string?> result = new();
        foreach (MediaIndexConfigKey key in Enum.GetValues<MediaIndexConfigKey>())
        {
            result[key] = rowsDict.ContainsKey(key.ToString()) ? rowsDict[key.ToString()].Value : null;
        }
        return result;
    }

    public async Task SetMediaIndexConfigAsync(Dictionary<MediaIndexConfigKey, string?> configs)
    {
        using QueryFactory db = Db();
        foreach (MediaIndexConfigKey key in configs.Keys)
        {
            int count = (await db.Query("MediaIndexConfig").Where("Key", key.ToString()).AsCount().GetAsync<int>()).First();
            if (count > 0)
            {
                await db.Query("MediaIndexConfig").Where("Key", key.ToString()).UpdateAsync(new
                {
                    Value = configs[key]
                });
            }
            else
            {
                await db.Query("MediaIndexConfig").InsertAsync(new
                {
                    Key = key.ToString(),
                    Value = configs[key]
                });
            }
        }
        await ApplyMediaIndexConfigAsync();
    }

    public Task<List<SearchResult>> SearchMediaIndexByQueryAsync(string query)
    {
        return MediaIndex.instance.Search(query);
    }

    public Task<SeriesDetails> GetSeriesDetailsAsync(string imdb)
    {
        return mediaIndex.GetSeriesDetailsAsync(imdb);
    }
}