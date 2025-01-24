namespace ReelGrab.Torents;

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
}