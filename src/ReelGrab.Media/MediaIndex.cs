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

    public async Task<List<SearchResult>> Search(string query)
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
}