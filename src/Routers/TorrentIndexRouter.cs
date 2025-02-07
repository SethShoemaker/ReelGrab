using ReelGrab.Core;
using ReelGrab.TorrentIndexes;

namespace ReelGrab.Web.Routers;

public class TorrentIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/torrent_index";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await TorrentIndexConfig.instance.GetTorrentIndexConfigAsync();
            await context.Response.WriteAsJsonAsync(config);
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<TorrentIndexConfigKey, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<TorrentIndexConfigKey, string?>>();
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
            await TorrentIndexConfig.instance.SetTorrentIndexConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await TorrentIndexConfig.instance.GetTorrentIndexConfigAsync());
        });

        app.MapGet($"{baseUrl}/status", async context => {

            bool jackettConnection;
            string jackettConnectionMessage;
            try {
                await TorrentIndex.instance.CheckConfig();
                jackettConnection = true;
                jackettConnectionMessage = "Jackett Connection Successful";
            } catch(Exception e){
                jackettConnection = false;
                jackettConnectionMessage = e.Message;
            }

            await context.Response.WriteAsJsonAsync(new {
                jackettConnection,
                jackettConnectionMessage
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