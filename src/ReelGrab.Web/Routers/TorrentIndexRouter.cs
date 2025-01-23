using ReelGrab.Core;

namespace ReelGrab.Web.Routers;

public class TorrentIndexRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/torrent_index";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await Application.instance.GetTorrentIndexConfigAsync();
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
            await Application.instance.SetTorrentIndexConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await Application.instance.GetTorrentIndexConfigAsync());
        });
    }
}