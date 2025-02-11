namespace ReelGrab.TorrentIndexes;

public interface ITorrentIndex
{
    public Task<bool> ConnectionGoodAsync();

    public record SearchResult(string Title, string IndexerName, string Url, Category Category, int Seeders, int Peers);

    public record SearchResponse(List<SearchResult> Results);

    public Task<SearchResponse> SearchMovie(string query);

    public Task<SearchResponse> SearchSeries(string query);
}