using System.Xml;
using ReelGrab.Database;
using ReelGrab.TorrentIndexes.Exceptions;
using SqlKata.Execution;

namespace ReelGrab.TorrentIndexes;

public class TorrentIndex : ITorrentIndex
{
    public static readonly TorrentIndex instance = new();

    private TorrentIndex()
    {
        Http = new();
        Http.Timeout = TimeSpan.FromSeconds(12);
    }

    public Uri? ApiUrl;

    public string? ApiKey;

    private readonly HttpClient Http;

    public async Task<bool> ConnectionGoodAsync()
    {
        EnsureTorrentIndexConfigured();
        try
        {
            string res = await Http.GetStringAsync($"{ApiUrl}api/v2.0/indexers/all/results/torznab/api?apikey={ApiKey}&t=caps");
            return res != "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<error code=\"100\" description=\"Invalid API Key\" />";
        }
        catch(HttpRequestException ex) when (ex.Message.Contains("Connection refused", StringComparison.CurrentCulture))
        {
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return false;
        }
    }

    public Task<ITorrentIndex.SearchResponse> SearchMovie(string query)
    {
        EnsureTorrentIndexConfigured();
        return Search(query, [Category.MOVIES]);
    }

    public Task<ITorrentIndex.SearchResponse> SearchSeries(string query)
    {
        EnsureTorrentIndexConfigured();
        return Search(query, [Category.TV]);
    }

    private async Task<ITorrentIndex.SearchResponse> Search(string query, IEnumerable<Category> categories)
    {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(await Http.GetStreamAsync($"{ApiUrl}api/v2.0/indexers/all/results/torznab/api?apikey={ApiKey}&t=search&cat={string.Join(",", categories.Select(c => (int)c))}&q={query}"));
        List<ITorrentIndex.SearchResult> searchResults = new();
        foreach (XmlNode item in xmlDocument.GetElementsByTagName("item"))
        {
            string title = item["title"]!.InnerText;
            string jackettIndexer = item["jackettindexer"]!.InnerText;
            string link = item["link"]!.InnerText;
            Category category = Enum.Parse<Category>(item["category"]!.InnerText);
            var torznabAttrs = item.ChildNodes.OfType<XmlNode>().AsQueryable().Where(node => node.Name == "torznab:attr");
            int seeders = int.Parse(torznabAttrs.Where(attr => attr.Attributes!["name"]!.Value == "seeders").Select(attr => attr.Attributes!["value"]!.Value).First());
            int peers = int.Parse(torznabAttrs.Where(attr => attr.Attributes!["name"]!.Value == "peers").Select(attr => attr.Attributes!["value"]!.Value).First());
            searchResults.Add(new(
                Title: title,
                IndexerName: jackettIndexer,
                Url: link,
                Category: category,
                Seeders: seeders,
                Peers: peers
            ));
        }
        return new(searchResults);
    }

    private record TorrentIndexConfigRow(string Key, string Value);

    public async Task InitializeConfigurationAsync()
    {
        using var db = Db.CreateConnection();
        var configs = await db.Query("TorrentIndexConfig").Select("Key", "Value").GetAsync<TorrentIndexConfigRow>();
        var apiUrl = configs.FirstOrDefault(c => c.Key == TorrentIndexConfigurationKey.API_URL.ToString())?.Value;
        ApiUrl = apiUrl != null ? new(apiUrl) : null;
        ApiKey = configs.FirstOrDefault(c => c.Key == TorrentIndexConfigurationKey.API_KEY.ToString())?.Value;
    }

    public async Task SetConfigurationAsync(Dictionary<TorrentIndexConfigurationKey, string?> configs)
    {
        // ensure no empty strings
        foreach (var key in configs.Keys)
        {
            if (configs[key] != null && configs[key]!.Length == 0)
            {
                configs[key] = null;
            }
        }
        using var db = Db.CreateConnection();
        foreach (TorrentIndexConfigurationKey key in configs.Keys)
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
        if (configs.TryGetValue(TorrentIndexConfigurationKey.API_URL, out string? newApiUrl))
        {
            ApiUrl = newApiUrl != null ? new(newApiUrl!) : null;
        }
        if (configs.TryGetValue(TorrentIndexConfigurationKey.API_KEY, out string? newApiKey))
        {
            ApiKey = newApiKey;
        }
    }

    public Task<Dictionary<TorrentIndexConfigurationKey, string?>> GetConfigurationAsync()
    {
        return Task.FromResult<Dictionary<TorrentIndexConfigurationKey, string?>>(new()
        {
            [TorrentIndexConfigurationKey.API_URL] = ApiUrl?.ToString(),
            [TorrentIndexConfigurationKey.API_KEY] = ApiKey
        });
    }

    private void EnsureTorrentIndexConfigured()
    {
        if (ApiUrl == null || ApiKey == null)
        {
            throw new TorrentIndexNotConfiguredException();
        }
    }
}