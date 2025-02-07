namespace ReelGrab.Media;

public enum MediaType
{
    MOVIE,
    SERIES
}

public record SearchResult(string SourceDisplayName, string Title, string ImdbId, MediaType MediaType, string? Poster);

public record SearchResponse(List<SearchResult> Results);

public record PaginatedSearchResponse(List<SearchResult> Results, int TotalCount);

public record MovieDetails(string Title, string ImdbId, string? PosterUrl, int Year, string Plot);

public record SeriesEpisodeDetails(int Number, string Title, string ImdbId);

public record SeriesSeasonDetails(int Number, List<SeriesEpisodeDetails> Episodes);

public record SeriesDetails(string Title, string ImdbId, string? PosterUrl, List<SeriesSeasonDetails> Seasons, int StartYear, int? EndYear, string Plot);

public interface IMediaDatabase
{
    public string DisplayName { get; }

    public Task<SearchResponse> SearchAsync(string query);

    public Task<MovieDetails> GetMovieDetailsByImdbIdAsync(string imdbId);

    public Task<SeriesDetails> GetSeriesDetailsByImdbIdAsync(string imdbId);

    public Task<MediaType> GetMediaTypeByImdbIdAsync(string imdbId);
}

public interface IMediaDatabasePaginated
{
    public Task<PaginatedSearchResponse> SearchPaginatedAsync(string query, int page);
}