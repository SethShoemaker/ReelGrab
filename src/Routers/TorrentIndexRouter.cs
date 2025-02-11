using ReelGrab.TorrentIndexes;

namespace ReelGrab.Web.Routers;

public class TorrentIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/torrent_index";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await TorrentIndex.instance.GetConfigurationAsync();
            await context.Response.WriteAsJsonAsync(config);
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<TorrentIndexConfigurationKey, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<TorrentIndexConfigurationKey, string?>>();
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
            await TorrentIndex.instance.SetConfigurationAsync(configs);
            await context.Response.WriteAsJsonAsync(await TorrentIndex.instance.GetConfigurationAsync());
        });

        app.MapGet($"{baseUrl}/status", async context => {
            bool jackettConnection = await TorrentIndex.instance.ConnectionGoodAsync();
            await context.Response.WriteAsJsonAsync(new {
                jackettConnection,
            });
        });

        app.MapGet($"{baseUrl}/search/movie", async context => {
            string? query = context.Request.Query["query"];
            if(string.IsNullOrWhiteSpace(query)){
                await context.Response.WriteAsJsonAsync(new {message = "Must provide query"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await TorrentIndex.instance.SearchMovie(query));
        });

        app.MapGet($"{baseUrl}/search/series", async context => {
            string? query = context.Request.Query["query"];
            if(string.IsNullOrWhiteSpace(query)){
                await context.Response.WriteAsJsonAsync(new {message = "Must provide query"});
                return;
            }
            await context.Response.WriteAsJsonAsync(await TorrentIndex.instance.SearchSeries(query));
        });
    }
}