using System.Text;
using Microsoft.AspNetCore.Mvc;
using ReelGrab.Core;
using ReelGrab.Web.Torrents.Models;

namespace ReelGrab.Web.Torrents;

[ApiController]
[Route("api/torrents")]
public class TorrentsController : ControllerBase
{
    [HttpGet]
    [Route("exists")]
    public async Task Exists([FromQuery] string url)
    {
        await Response.WriteAsJsonAsync(new { Exists = await Application.instance.TorrentWithUrlExistsAsync(url)});
    }

    [HttpPost]
    public async Task Add([FromBody] AddRequest request)
    {
        int id = await Application.instance.AddTorrentAsync(request.Url, request.Source);
        await Response.WriteAsJsonAsync(new { Message = $"torrent has been created with id {id}", Id = id});
    }

    [HttpGet]
    [Route("inspect")]
    public async Task Inspect([FromQuery] string url)
    {
        if(! await Application.instance.TorrentWithUrlExistsAsync(url))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await Response.WriteAsJsonAsync(await Application.instance.InspectTorrentWithUrlAsync(url));
    }
}