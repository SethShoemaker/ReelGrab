using Microsoft.AspNetCore.Mvc;
using ReelGrab.Core;

namespace ReelGrab.Web.Movies;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    [HttpPost]
    public async Task Add([FromBody] AddRequest request)
    {
        int id = await Application.instance.AddMovieAsync(request.ImdbId, request.Name, request.Description, request.Poster, request.Year!.Value, request.Wanted!.Value);
        Response.StatusCode = StatusCodes.Status201Created;
        await Response.WriteAsJsonAsync(new { Message = $"Added movie, has id of {id}", Id = id });
    }

    [HttpGet("{imdbId}/exists")]
    public async Task Exists([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(new { Exists = await Application.instance.MovieWithImdbIdExistsAsync(imdbId)});
    }

    [HttpPost("{imdbId}/wanted")]
    public async Task SetWanted([FromRoute] string imdbId, [FromBody] SetWantedRequest request)
    {
        await Application.instance.SetMovieWantedAsync(imdbId, request.Wanted!.Value);
        await Response.WriteAsJsonAsync(new { Message = $"{imdbId} is now {(request.Wanted!.Value ? "" : "not ")}wanted" });
    }

    [HttpPost("{imdbId}/cinematic_cut_torrent")]
    public async Task SetCinematicCutTorrent([FromRoute] string imdbId, [FromBody] SetCinematicCutTorrentRequest request)
    {
        int torrentId = await Application.instance.TorrentWithUrlExistsAsync(request.TorrentUrl)
            ? await Application.instance.GetTorrentIdByUrlAsync(request.TorrentUrl)
            : await Application.instance.AddTorrentAsync(request.TorrentUrl, request.TorrentSource);

        int torrentFileId = await Application.instance.GetTorrentFileIdByTorrentIdAndPathAsync(torrentId, request.TorrentFilePath);

        await Application.instance.SetMovieCinematicCutTorrentAsync(imdbId, torrentId, torrentFileId);
        await Response.WriteAsJsonAsync(new { message = $"movie with imdbId {imdbId} has its cinematic cut torrent set" });
    }

    [HttpGet("{imdbId}/cinematic_cut_torrent")]
    public async Task GetCinematicCutTorrent([FromRoute] string imdbId)
    {
        if(! await Application.instance.MovieWithImdbIdExistsAsync(imdbId))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { message = $"movie with imdbid {imdbId} does not exist"});
            return;
        }
        await Response.WriteAsJsonAsync(await Application.instance.GetMovieCinematicCutTorrentAsync(imdbId));
    }

    [HttpPost("{imdbId}/storage_locations")]
    public async Task SetStorageLocations([FromRoute] string imdbId, [FromBody] SetStorageLocationsRequest request)
    {
        await Application.instance.SetMovieStorageLocationsAsync(imdbId, request.StorageLocations);
        await Response.WriteAsJsonAsync(new { message = $"movie with imdbid {imdbId} has new storage locations"});
    }

    [HttpGet("{imdbId}/storage_locations")]
    public async Task GetStorageLocations([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(new { StorageLocations = await Application.instance.GetMovieStorageLocationsAsync(imdbId)});
    }

    [HttpGet("in_progress")]
    public async Task GetInProgress()
    {
        await Response.WriteAsJsonAsync(await Application.instance.GetMoviesInProgressAsync());
    }
}