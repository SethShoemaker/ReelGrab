using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public ITorrentClient? torrentClient = null;

    public record TorrentClientConfigRow(string Key, string Value);

    public async Task ApplyTorrentClientConfigAsync()
    {
        using QueryFactory db = Db();
        var configs = await db.Query("TorrentClientConfig").Select("Key", "Value").GetAsync<TorrentClientConfigRow>();
        var chosenClient = configs.FirstOrDefault(c => c.Key == TorrentClientConfigKey.CHOSEN_CLIENT.ToString())?.Value;
        if (chosenClient == null)
        {
            torrentClient = null;
        }
        switch (chosenClient)
        {
            case "Transmission":
                var hostStr = configs.FirstOrDefault(c => c.Key == TorrentClientConfigKey.TRANSMISSION_HOST.ToString())?.Value;
                var portStr = configs.FirstOrDefault(c => c.Key == TorrentClientConfigKey.TRANSMISSION_PORT.ToString())?.Value;
                if (hostStr != null && portStr != null && int.TryParse(portStr, out int portInt))
                {
                    try
                    {
                        torrentClient = await Transmission.CreateAsync(hostStr, portInt);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                break;
            default:
                break;
        }
    }

    public async Task<Dictionary<TorrentClientConfigKey, string?>> GetTorrentClientConfigAsync()
    {
        Dictionary<string, TorrentClientConfigRow> rowsDict;
        using (var db = Db())
        {
            rowsDict = (await db.Query("TorrentClientConfig").Select("Key", "Value").GetAsync<TorrentClientConfigRow>()).ToDictionary(r => r.Key);
        }
        Dictionary<TorrentClientConfigKey, string?> result = new();
        foreach (TorrentClientConfigKey key in Enum.GetValues<TorrentClientConfigKey>())
        {
            result[key] = rowsDict.ContainsKey(key.ToString()) ? rowsDict[key.ToString()].Value : null;
        }
        return result;
    }

    public async Task SetTorrentClientConfigAsync(Dictionary<TorrentClientConfigKey, string?> configs)
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
        foreach (TorrentClientConfigKey key in configs.Keys)
        {
            int count = (await db.Query("TorrentClientConfig").Where("Key", key.ToString()).AsCount().GetAsync<int>()).First();
            if (count > 0)
            {
                await db.Query("TorrentClientConfig").Where("Key", key.ToString()).UpdateAsync(new
                {
                    Value = configs[key]
                });
            }
            else
            {
                await db.Query("TorrentClientConfig").InsertAsync(new
                {
                    Key = key.ToString(),
                    Value = configs[key]
                });
            }
        }
        await ApplyTorrentClientConfigAsync();
    }

    public string? GetTorrentClientName()
    {
        return torrentClient?.Name;
    }
}