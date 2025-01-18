namespace ReelGrab.Media;

public record SearchResult(string SourceDisplayName, string Title, string ImdbId);

public record SearchResponse(List<SearchResult> Results);

public record PaginatedSearchResponse(List<SearchResult> Results, int TotalCount);

public interface IMediaDatabase
{
    public string DisplayName { get; }

    public Task<SearchResponse> SearchAsync(string query);
}

public interface IMediaDatabasePaginated
{
    public Task<PaginatedSearchResponse> SearchPaginatedAsync(string query, int page);
}