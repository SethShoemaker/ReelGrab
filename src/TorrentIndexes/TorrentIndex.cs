using System.Xml;
using ReelGrab.TorrentIndexes.Exceptions;
using SqlKata.Execution;

namespace ReelGrab.TorrentIndexes;

public class TorrentIndex : ITorrentIndex
{
    public static readonly TorrentIndex instance = new();

    private TorrentIndex()
    {
        Http = new();
        Http.Timeout = TimeSpan.FromSeconds(60);
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

    private void EnsureTorrentIndexConfigured()
    {
        if (ApiUrl == null || ApiKey == null)
        {
            throw new TorrentIndexNotConfiguredException();
        }
    }
}