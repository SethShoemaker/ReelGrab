using Microsoft.AspNetCore.Mvc;

namespace ReelGrab.Web.TorrentIndex;

[ApiController]
[Route("api/torrent_index")]
public class TorrentIndexController : ControllerBase
{
    [HttpGet("search/series")]
    public async Task SearchSeries([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await Response.WriteAsJsonAsync(new { message = "Must provide query" });
            return;
        }
        await Response.WriteAsJsonAsync(await TorrentIndexes.TorrentIndex.instance.SearchSeries(query));
    }

    [HttpGet("search/movie")]
    public async Task SearchMovie([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await Response.WriteAsJsonAsync(new { message = "Must provide query" });
            return;
        }
        await Response.WriteAsJsonAsync(await TorrentIndexes.TorrentIndex.instance.SearchMovie(query));
    }
}