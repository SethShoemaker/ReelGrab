using ReelGrab.TorrentIndexes;
using ReelGrab.Utils;

namespace ReelGrab.Web.Routers;

public class TorrentIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/torrent_index";

        app.MapGet($"{baseUrl}/config", async context => {
            await context.Response.WriteAsJsonAsync(new {
                api_url = await Configuration.TorrentIndex.instance.GetJackettApiUrl(),
                api_key = await Configuration.TorrentIndex.instance.GetJackettApiKey(),
            });
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<string, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<string, string?>>();
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
            if(configs.TryGetValue("api_url", out string? jackettApiUrl))
            {
                await Configuration.TorrentIndex.instance.SetJackettApiUrl(jackettApiUrl);
            }
            if(configs.TryGetValue("api_key", out string? jackettApiKey))
            {
                await Configuration.TorrentIndex.instance.SetJackettApiKey(jackettApiKey);
            }
            await context.Response.WriteAsJsonAsync(new {
                api_url = await Configuration.TorrentIndex.instance.GetJackettApiUrl(),
                api_key = await Configuration.TorrentIndex.instance.GetJackettApiKey(),
            });
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