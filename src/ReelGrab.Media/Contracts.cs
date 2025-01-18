namespace ReelGrab.Media;

public record SearchResult(string SourceDisplayName, string Title, string ImdbId);

public record SearchResponse(List<SearchResult> Results);

public record PaginatedSearchResponse(List<SearchResult> Results, int TotalCount);

internal interface IMediaDatabase
{
    public Task<SearchResponse> SearchAsync(string query);
}

internal interface IMediaDatabasePaginated
{
    public Task<PaginatedSearchResponse> SearchPaginatedAsync(string query, int page);
}