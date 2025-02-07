
using ReelGrab.Core;

namespace ReelGrab.Web.Routers;

public class StorageGatewayRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/storage_gateway";

        app.MapGet($"{baseUrl}/config", async context => {
            var config = await Application.instance.GetStorageGatewayConfigAsync();
            await context.Response.WriteAsJsonAsync(config);
        });

        app.MapPost($"{baseUrl}/config", async context => {
            Dictionary<StorageGatewayConfigKey, string?>? configs;
            try {
                configs = await context.Request.ReadFromJsonAsync<Dictionary<StorageGatewayConfigKey, string?>>();
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
            await Application.instance.SetStorageGatewayConfigAsync(configs);
            await context.Response.WriteAsJsonAsync(await Application.instance.GetStorageGatewayConfigAsync());
        });

        app.MapGet($"{baseUrl}/storage_locations", async context => {
            await context.Response.WriteAsJsonAsync(Application.instance.storageGateway.StorageLocations);
        });
    }
}