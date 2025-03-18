using ReelGrab.MediaIndexes;

namespace ReelGrab.Web.Routers;

public class MediaIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/media_index";

        app.MapGet($"{baseUrl}/config", async context => {
            await context.Response.WriteAsJsonAsync(new {
                omdb_api_key = await Configuration.MediaIndex.instance.GetOmdbApiKey()
            });
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<string, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<string, string?>>();
            }
            catch (System.Text.Json.JsonException)
            {
                await context.Response.WriteAsJsonAsync(new {message = "error while decoding config"});
                return;
            }
            if(configs == null)
            {
                await context.Response.WriteAsJsonAsync(new {message = "error while decoding config"});
                return;
            }
            if(configs.TryGetValue("omdb_api_key", out string? omdbApiKey))
            {
                await Configuration.MediaIndex.instance.SetOmdbApiKey(omdbApiKey);
            }
            await context.Response.WriteAsJsonAsync(new {
                omdb_api_key = await Configuration.MediaIndex.instance.GetOmdbApiKey()
            });
        });

        app.MapGet($"{baseUrl}/databases", async context => {
            await context.Response.WriteAsJsonAsync(MediaIndex.instance.MediaDatabases);
        });

        app.MapGet($"{baseUrl}/search", async context => {
            string? query = context.Request.Query["query"];
            if(string.IsNullOrWhiteSpace(query)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide query"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndexes.MediaIndex.instance.SearchAsync(query));
        });

        app.MapGet($"{baseUrl}/type", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            MediaType mediaType = await MediaIndexes.MediaIndex.instance.GetMediaTypeByImdbIdAsync(imdbId);
            await context.Response.WriteAsJsonAsync(new {type = mediaType});
        });

        app.MapGet($"{baseUrl}/movie/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndexes.MediaIndex.instance.GetMovieDetailsByImdbIdAsync(imdbId));
        });

        app.MapGet($"{baseUrl}/series/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndexes.MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId));
        });
    }
}