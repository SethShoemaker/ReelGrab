using System.Collections.ObjectModel;
using ReelGrab.Media.Databases;

namespace ReelGrab.Media;

public class MediaIndex
{
    private MediaIndex(){}

    public static readonly MediaIndex instance = new MediaIndex();

    private List<IMediaDatabase> mediaDatabases = [];

    private Exception noMediaDatabasesConfigured = new Exception("tried searching media index when no media databases were configured");

    public ReadOnlyCollection<IMediaDatabase> MediaDatabases
    {
        get
        {
            return mediaDatabases.AsReadOnly();
        }
    }

    public void AddOmdbDatabase(string apiKey)
    {
        RemoveOmdbDatabase();
        mediaDatabases.Add(new OmdbMediaDatabase(apiKey));
    }

    public void RemoveOmdbDatabase()
    {
        mediaDatabases = mediaDatabases.Where(md => md.GetType() != typeof(OmdbMediaDatabase)).ToList();
    }

    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        if(mediaDatabases.Count == 0){
            throw noMediaDatabasesConfigured;
        }
        if(query.Length == 0){
            return [];
        }
        List<SearchResult> results = [];
        foreach(var mediaDatabase in mediaDatabases){
            results.AddRange((await mediaDatabase.SearchAsync(query)).Results);
        }
        return results;
    }

    public Task<MediaType> GetMediaTypeByImdbIdAsync(string imdbId)
    {
        return MediaDatabases.First().GetMediaTypeByImdbIdAsync(imdbId);
    }

    public Task<MovieDetails> GetMovieDetailsByImdbIdAsync(string imdbId)
    {
        return MediaDatabases.First().GetMovieDetailsByImdbIdAsync(imdbId);
    }

    public Task<SeriesDetails> GetSeriesDetailsByImdbIdAsync(string imdbId)
    {
        return MediaDatabases.First().GetSeriesDetailsByImdbIdAsync(imdbId);
    }
}