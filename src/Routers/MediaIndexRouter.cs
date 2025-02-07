using ReelGrab.MediaIndexes;

namespace ReelGrab.Web.Routers;

public class MediaIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/media_index";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await MediaIndexConfig.instance.GetMediaIndexConfigAsync();
            await context.Response.WriteAsJsonAsync(config);
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<MediaIndexConfigKey, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<MediaIndexConfigKey, string?>>();
            }
            catch (System.Text.Json.JsonException)
            {
                await context.Response.WriteAsJsonAsync(new {message = "Error while decoding config"});
                return;
            }
            if(configs == null){
                await context.Response.WriteAsJsonAsync(new {message = "Error while decoding config"});
                return;
            }
            await MediaIndexConfig.instance.SetMediaIndexConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await MediaIndexConfig.instance.GetMediaIndexConfigAsync());
        });

        app.MapGet($"{baseUrl}/databases", async context => {
            await context.Response.WriteAsJsonAsync(MediaIndexConfig.instance.mediaIndex.MediaDatabases);
        });

        app.MapGet($"{baseUrl}/search", async context => {
            string? query = context.Request.Query["query"];
            if(string.IsNullOrWhiteSpace(query)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide query"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndex.instance.SearchAsync(query));
        });

        app.MapGet($"{baseUrl}/type", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            MediaType mediaType = await MediaIndex.instance.GetMediaTypeByImdbIdAsync(imdbId);
            await context.Response.WriteAsJsonAsync(new {type = mediaType});
        });

        app.MapGet($"{baseUrl}/movie/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndex.instance.GetMovieDetailsByImdbIdAsync(imdbId));
        });

        app.MapGet($"{baseUrl}/series/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId));
        });
    }
}