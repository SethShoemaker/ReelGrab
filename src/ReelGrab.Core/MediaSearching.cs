using ReelGrab.Media;

namespace ReelGrab.Core;

public partial class Application
{
    public Task<List<SearchResult>> SearchMediaIndexByQuery(string query)
    {
        return MediaIndex.instance.Search(query);
    }
}