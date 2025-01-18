using ReelGrab.Media.Databases;

namespace ReelGrab.Media;

public class MediaIndex
{
    public static void AddOmdbDatabase(string apiKey)
    {
        if(instance.mediaDatabases.Count(mediaDatabase => mediaDatabase.GetType() == typeof(OmdbMediaDatabase)) > 0){
            throw new InvalidOperationException("Omdb database already exists");
        }
        instance.mediaDatabases.Add(new OmdbMediaDatabase(apiKey));
    }

    private MediaIndex(){}

    public static readonly MediaIndex instance = new MediaIndex();

    private List<IMediaDatabase> mediaDatabases = [];

    private Exception noMediaDatabasesConfigured = new Exception("tried searching media index when no media databases were configured");

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