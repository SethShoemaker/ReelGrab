using System.Xml;

namespace ReelGrab.TorrentIndexes;

public partial class TorrentIndex
{
    private TorrentIndex(){
        http = new HttpClient();
    }

    public static readonly TorrentIndex instance = new TorrentIndex();

    public Uri? ApiUrl;

    public string? ApiKey;

    private HttpClient http;

    private record GetIndexersIndexerCap(string ID, string Name);

    private record GetIndexersIndexer(string id, string description, string type, bool configured, string site_link, string language);

    public async Task<bool> CheckConfig()
    {
        string res = await http.GetStringAsync($"{ApiUrl}api/v2.0/indexers/all/results/torznab/api?apikey={ApiKey}&t=caps");
        if(res == "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<error code=\"100\" description=\"Invalid API Key\" />"){
            throw new Exception("Invalid Api Key");
        }
        return true;
    }

    public Task<SearchResponse> SearchMovie(string query)
    {
        return Search(query, [Category.MOVIES]);
    }

    public Task<SearchResponse> SearchSeries(string query)
    {
        return Search(query, [Category.TV]);
    }

    private async Task<SearchResponse> Search(string query, IEnumerable<Category> categories)
    {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(await http.GetStreamAsync($"{ApiUrl}api/v2.0/indexers/all/results/torznab/api?apikey={ApiKey}&t=search&cat={string.Join(",", categories.Select(c => (int)c))}&q={query}"));
        List<SearchResult> searchResults = new();
        foreach(XmlNode item in xmlDocument.GetElementsByTagName("item")){
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
}