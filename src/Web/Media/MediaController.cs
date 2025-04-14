using Microsoft.AspNetCore.Mvc;
using ReelGrab.MediaIndexes;

namespace ReelGrab.Web.Media;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    [HttpGet("config")]
    public async Task GetConfig()
    {
        await Response.WriteAsJsonAsync(new
        {
            omdb_api_key = await Configuration.MediaIndex.instance.GetOmdbApiKey()
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
            await Response.WriteAsJsonAsync(new { message = "error while decoding config" });
            return;
        }
        if (configs == null)
        {
            await Response.WriteAsJsonAsync(new { message = "error while decoding config" });
            return;
        }
        if (configs.TryGetValue("omdb_api_key", out string? omdbApiKey))
        {
            await Configuration.MediaIndex.instance.SetOmdbApiKey(omdbApiKey);
        }
        await Configuration.MediaIndex.instance.Apply();
        await Response.WriteAsJsonAsync(new
        {
            omdb_api_key = await Configuration.MediaIndex.instance.GetOmdbApiKey()
        });
    }

    [HttpGet("databases")]
    public async Task GetDatabases()
    {
        await Response.WriteAsJsonAsync(MediaIndex.instance.MediaDatabases);
    }

    [HttpGet("search/movies_and_series")]
    public async Task SearchMoviesAndSeries([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { message = "did not provide query" });
            return;
        }
        await Response.WriteAsJsonAsync(await MediaIndex.instance.SearchAsync(query));
    }

    [HttpGet("movies/{imdbId}/details")]
    public async Task GetMovieDetails([FromRoute] string imdbId)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
            return;
        }
        await Response.WriteAsJsonAsync(await MediaIndex.instance.GetMovieDetailsByImdbIdAsync(imdbId));
    }

    [HttpGet("series/{imdbId}/details")]
    public async Task GetSeriesDetails([FromRoute] string imdbId)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
            return;
        }
        await Response.WriteAsJsonAsync(await MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId));
    }
}