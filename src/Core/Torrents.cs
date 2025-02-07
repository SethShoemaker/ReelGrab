using ReelGrab.TorrentIndexes;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public readonly TorrentIndex torrentIndex = TorrentIndex.instance;

    public record TorrentIndexConfigRow(string Key, string Value);

    public async Task ApplyTorrentIndexConfigAsync()
    {
        using QueryFactory db = Db();
        var configs = await db.Query("TorrentIndexConfig").Select("Key", "Value").GetAsync<TorrentIndexConfigRow>();

        void ApplyApiUrl(IEnumerable<TorrentIndexConfigRow> rows)
        {
            var row = rows.FirstOrDefault(c => c.Key == TorrentIndexConfigKey.API_URL.ToString());
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                torrentIndex.ApiUrl = null;
            }
            else
            {
                torrentIndex.ApiUrl = new Uri(row.Value);
            }
        }

        void ApplyApiKey(IEnumerable<TorrentIndexConfigRow> rows)
        {
            var row = rows.FirstOrDefault(c => c.Key == TorrentIndexConfigKey.API_KEY.ToString());
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                torrentIndex.ApiKey = null;
            }
            else
            {
                torrentIndex.ApiKey = row.Value;
            }
        }

        ApplyApiUrl(configs);
        ApplyApiKey(configs);
    }

    public async Task<Dictionary<TorrentIndexConfigKey, string?>> GetTorrentIndexConfigAsync()
    {
        Dictionary<string, TorrentIndexConfigRow> rowsDict;
        using (var db = Db())
        {
            rowsDict = (await db.Query("TorrentIndexConfig").Select("Key", "Value").GetAsync<TorrentIndexConfigRow>()).ToDictionary(r => r.Key);
        }
        Dictionary<TorrentIndexConfigKey, string?> result = new();
        foreach (TorrentIndexConfigKey key in Enum.GetValues<TorrentIndexConfigKey>())
        {
            result[key] = rowsDict.ContainsKey(key.ToString()) ? rowsDict[key.ToString()].Value : null;
        }
        return result;
    }

    public async Task SetTorrentIndexConfigAsync(Dictionary<TorrentIndexConfigKey, string?> configs)
    {
        // ensure no empty strings
        foreach (var key in configs.Keys)
        {
            if (configs[key] != null && configs[key]!.Length == 0)
            {
                configs[key] = null;
            }
        }
        using QueryFactory db = Db();
        foreach (TorrentIndexConfigKey key in configs.Keys)
        {
            int count = (await db.Query("TorrentIndexConfig").Where("Key", key.ToString()).AsCount().GetAsync<int>()).First();
            if (count > 0)
            {
                await db.Query("TorrentIndexConfig").Where("Key", key.ToString()).UpdateAsync(new
                {
                    Value = configs[key]
                });
            }
            else
            {
                await db.Query("TorrentIndexConfig").InsertAsync(new
                {
                    Key = key.ToString(),
                    Value = configs[key]
                });
            }
        }
        await ApplyTorrentIndexConfigAsync();
    }
}