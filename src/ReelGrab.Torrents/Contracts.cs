namespace ReelGrab.Torrents;

public record SearchResult(string Title, string IndexerName, string Url, Category Category, int Seeders, int Peers);

public record SearchResponse(List<SearchResult> Results);