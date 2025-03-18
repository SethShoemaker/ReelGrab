using ReelGrab.Storage;

namespace ReelGrab.Web.Routers;

public class StorageGatewayRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/storage_gateway";

        app.MapGet($"{baseUrl}/config", async context => {
            await context.Response.WriteAsJsonAsync(new {
                local_directories = string.Join(',', await Configuration.StorageGateway.instance.GetLocalDirectories())
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
            if(configs.TryGetValue("local_directories", out string? localDirectories))
            {
                await Configuration.StorageGateway.instance.SetLocalDirectories(localDirectories?.Split(',').ToList() ?? []);
            }
            await context.Response.WriteAsJsonAsync(new {
                local_directories = string.Join(',', await Configuration.StorageGateway.instance.GetLocalDirectories())
            });
        });

        app.MapGet($"{baseUrl}/storage_locations", async context => {
            await context.Response.WriteAsJsonAsync(StorageGateway.instance.StorageLocations);
        });
    }
}