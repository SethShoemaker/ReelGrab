using Microsoft.AspNetCore.Mvc;
using ReelGrab.Storage;

namespace ReelGrab.Web.Storage;

[ApiController]
[Route("storage")]
public class StorageController : ControllerBase
{
    [HttpGet("config")]
    public async Task GetConfig()
    {
        await Response.WriteAsJsonAsync(new
        {
            local_directories = string.Join(',', await Configuration.StorageGateway.instance.GetLocalDirectories())
        });
    }

    [HttpPut("config")]
    public async Task SetConfig()
    {
        Dictionary<string, string?>? configs;
        try
        {
            configs = await Request.ReadFromJsonAsync<Dictionary<string, string?>>();
        }
        catch (System.Text.Json.JsonException)
        {
            await Response.WriteAsJsonAsync(new { message = "Error while decoding config" });
            return;
        }
        if (configs == null)
        {
            await Response.WriteAsJsonAsync(new { message = "Error while decoding config" });
            return;
        }
        if (configs.TryGetValue("local_directories", out string? localDirectories))
        {
            await Configuration.StorageGateway.instance.SetLocalDirectories(localDirectories?.Split(',').ToList() ?? []);
        }
        await Response.WriteAsJsonAsync(new
        {
            local_directories = string.Join(',', await Configuration.StorageGateway.instance.GetLocalDirectories())
        });
    }

    [HttpGet("locations")]
    public async Task GetLocations()
    {
        await Response.WriteAsJsonAsync(StorageGateway.instance.StorageLocations);
    }
}