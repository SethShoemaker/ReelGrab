using Microsoft.AspNetCore.Mvc;

namespace ReelGrab.Web.TorrentIndex;

[ApiController]
[Route("torrent_index")]
public class TorrentIndexController : ControllerBase
{
    [HttpGet("config")]
    public async Task GetConfig()
    {
        await Response.WriteAsJsonAsync(new
        {
            api_url = await Configuration.TorrentIndex.instance.GetJackettApiUrl(),
            api_key = await Configuration.TorrentIndex.instance.GetJackettApiKey(),
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
        if (configs.TryGetValue("api_url", out string? jackettApiUrl))
        {
            await Configuration.TorrentIndex.instance.SetJackettApiUrl(jackettApiUrl);
        }
        if (configs.TryGetValue("api_key", out string? jackettApiKey))
        {
            await Configuration.TorrentIndex.instance.SetJackettApiKey(jackettApiKey);
        }
        await Response.WriteAsJsonAsync(new
        {
            api_url = await Configuration.TorrentIndex.instance.GetJackettApiUrl(),
            api_key = await Configuration.TorrentIndex.instance.GetJackettApiKey(),
        });
    }

    [HttpGet("status")]
    public async Task GetStatus()
    {
        bool jackettConnection = await TorrentIndexes.TorrentIndex.instance.ConnectionGoodAsync();
        await Response.WriteAsJsonAsync(new
        {
            jackettConnection,
        });
    }

    [HttpGet("search/series")]
    public async Task SearchSeries([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await Response.WriteAsJsonAsync(new { message = "Must provide query" });
            return;
        }
        await Response.WriteAsJsonAsync(await TorrentIndexes.TorrentIndex.instance.SearchMovie(query));
    }

    [HttpGet("search/movie")]
    public async Task SearchMovie([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await Response.WriteAsJsonAsync(new { message = "Must provide query" });
            return;
        }
        await Response.WriteAsJsonAsync(await TorrentIndexes.TorrentIndex.instance.SearchSeries(query));
    }
}