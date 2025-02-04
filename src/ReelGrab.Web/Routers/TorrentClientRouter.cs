
using ReelGrab.Core;

namespace ReelGrab.Web.Routers;

public class TorrentClientRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/torrent_client";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await Application.instance.GetTorrentClientConfigAsync();
            await context.Response.WriteAsJsonAsync(config);
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<TorrentClientConfigKey, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<TorrentClientConfigKey, string?>>();
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
            await Application.instance.SetTorrentClientConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await Application.instance.GetTorrentClientConfigAsync());
        });

        app.MapGet($"{baseUrl}/torrent_client_name", async context => {
            await context.Response.WriteAsJsonAsync(new {name = Application.instance.GetTorrentClientName()});
        });
    }
}