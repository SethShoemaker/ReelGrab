using ReelGrab.Core;
using ReelGrab.Media;
using ReelGrab.Web.Routers;

namespace ReelGrab.Web;

public class MediaIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/media_index";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await Application.instance.GetMediaIndexConfigAsync();
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
            await Application.instance.SetMediaIndexConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await Application.instance.GetMediaIndexConfigAsync());
        });

        app.MapGet($"{baseUrl}/databases", async context => {
            await context.Response.WriteAsJsonAsync(Application.instance.mediaIndex.MediaDatabases);
        });

        app.MapGet($"{baseUrl}/search", async context => {
            string? query = context.Request.Query["query"];
            if(string.IsNullOrWhiteSpace(query)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide query"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await Application.instance.SearchMediaIndexAsync(query));
        });

        app.MapGet($"{baseUrl}/type", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            MediaType mediaType = await Application.instance.GetMediaTypeByImdbIdAsync(imdbId);
            await context.Response.WriteAsJsonAsync(new {type = mediaType});
        });

        app.MapGet($"{baseUrl}/movie/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await Application.instance.GetMovieDetailsByImdbIdAsync(imdbId));
        });

        app.MapGet($"{baseUrl}/series/details", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId)){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {message = "did not provide imdbId"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await Application.instance.GetSeriesDetailsByImdbIdAsync(imdbId));
        });
    }
}