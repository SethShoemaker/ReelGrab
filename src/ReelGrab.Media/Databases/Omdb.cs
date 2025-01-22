
using System.Net.Http.Json;
using System.Web;

namespace ReelGrab.Media.Databases;

public class OmdbMediaDatabase: IMediaDatabase, IMediaDatabasePaginated
{
    private string apiKey;

    private HttpClient http;

    internal OmdbMediaDatabase(string apiKey)
    {
        this.apiKey = apiKey;
        http = new HttpClient();
        http.BaseAddress = new Uri("http://www.omdbapi.com/");
    }

    private string displayName = "OMDb";

    public string DisplayName
    {
        get {
            return displayName;
        }
    }

    record ApiSearchResponseItem(string Title, string Year, string imdbID, string Type, string Poster);

    record ApiSearchResponse(List<ApiSearchResponseItem>? Search, string Response, string? Error, string? totalResults);

    public async Task<SearchResponse> SearchAsync(string query)
    {
        var body = await http.GetFromJsonAsync<ApiSearchResponse>($"?s={HttpUtility.UrlEncode(query)}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
        var results = body.Response == "False" ? new List<SearchResult>() : body.Search!.Select(sbi => new SearchResult(SourceDisplayName: "OMDb", Title: sbi.Title, ImdbId: sbi.imdbID, sbi.Type == "series" ? MediaType.SERIES : MediaType.MOVIE, Poster: sbi.Poster)).ToList();
        return new SearchResponse(results);
    }

    public async Task<PaginatedSearchResponse> SearchPaginatedAsync(string query, int page)
    {
        var body = await http.GetFromJsonAsync<ApiSearchResponse>($"?s={HttpUtility.UrlEncode(query)}&page={page}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
        var totalCount = body.totalResults != null ? int.Parse(body.totalResults) : 0;
        var results = body.Search != null ? body.Search.Select(sbi => new SearchResult(SourceDisplayName: "OMDb", Title: sbi.Title, ImdbId: sbi.imdbID, sbi.Type == "series" ? MediaType.SERIES : MediaType.MOVIE, Poster: sbi.Poster)).ToList() : new List<SearchResult>();
        return new PaginatedSearchResponse(results, totalCount);
    }

    public async Task<MediaType> GetMediaTypeByImdbIdAsync(string imdbId)
    {
        var body = await http.GetFromJsonAsync<ApiGetDetailsResponse>($"?i={HttpUtility.UrlEncode(imdbId)}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
        if(body.Response != "True"){
            throw new Exception($"error which getting series with IMDb id {imdbId}");
        }
        if(string.IsNullOrWhiteSpace(body.Type)){
            throw new Exception($"error which getting series with IMDb id {imdbId}");
        }
        return body.Type switch
        {
            "series" => MediaType.SERIES,
            "movie" => MediaType.MOVIE,
            _ => throw new Exception($"received weird type from api: ${body.Type}"),
        };
    }

    record ApiGetDetailsResponse(string Response, string? Error, string? Title, string? Year, string? Poster, int? totalSeasons, string? Type);

    public async Task<MovieDetails> GetMovieDetailsByImdbIdAsync(string imdbId)
    {
        var body = await http.GetFromJsonAsync<ApiGetDetailsResponse>($"?i={HttpUtility.UrlEncode(imdbId)}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
        if(body.Response != "True"){
            throw new Exception($"error which getting series with IMDb id {imdbId}");
        }
        if(body.Type != "movie"){
            throw new Exception($"{imdbId} has type {body.Type}, not movie");
        }
        return new MovieDetails(body.Title!, imdbId, body.Poster);
    }

    record ApiGetSeriesSeasonResponseEpisode(string Title, string Episode, string imdbID);

    record ApiGetSeriesSeasonResponse(string Season, List<ApiGetSeriesSeasonResponseEpisode> Episodes);

    public async Task<SeriesDetails> GetSeriesDetailsByImdbIdAsync(string imdbId)
    {
        var body = await http.GetFromJsonAsync<ApiGetDetailsResponse>($"?i={HttpUtility.UrlEncode(imdbId)}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
        if(body.Response != "True"){
            throw new Exception($"error which getting series with IMDb id {imdbId}");
        }
        if(body.Type != "series"){
            throw new Exception($"{imdbId} has type {body.Type}, not series");
        }
        List<SeriesSeasonDetails> seasons = new();
        for (int i = 0; i < body.totalSeasons; i++)
        {
            var seasonBody = await http.GetFromJsonAsync<ApiGetSeriesSeasonResponse>($"?i={HttpUtility.UrlEncode(imdbId)}&season=${i + 1}&apikey={HttpUtility.UrlEncode(apiKey)}") ?? throw new Exception("OMDb: API did not return correct response");
            seasons.Add(new SeriesSeasonDetails(
                Number: int.Parse(seasonBody.Season),
                Episodes: seasonBody.Episodes.Select(e => new SeriesEpisodeDetails(
                    Number: int.Parse(e.Episode),
                    Title: e.Title,
                    ImdbId: e.imdbID
                )).ToList()
            ));
        }
        return new SeriesDetails(body.Title!, imdbId, body.Poster, seasons);
    }
}