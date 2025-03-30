using Microsoft.AspNetCore.Mvc;
using ReelGrab.Core;

namespace ReelGrab.Web.Series;

[ApiController]
[Route("api/series")]
public class SeriesController : ControllerBase
{
    [HttpPost]
    public async Task Add([FromBody] AddRequest request)
    {
        await Application.instance.AddSeriesAsync(
            request.ImdbId,
            request.Name,
            request.Description,
            request.Poster,
            request.StartYear!.Value,
            request.EndYear,
            request.Seasons.Select(s => new Application.AddSeriesAsyncSeason(
                s.Number,
                null,
                null,
                s.Episodes.Select(e => new Application.AddSeriesAsyncEpisode(
                    e.Number!.Value,
                    e.Name,
                    e.ImdbId,
                    null,
                    null,
                    e.Wanted!.Value
                )).ToList()
            )).ToList()
        );
    }

    [HttpGet("{imdbId}/exists")]
    public async Task Exists([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(new { exists = await Application.instance.SeriesWithImdbIdExistsAsync(imdbId)});
    }

    [HttpGet("{imdbId}/wanted")]
    public async Task GetWantedInfo([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(await Application.instance.GetSeriesWantedInfoAsync(imdbId));
    }

    [HttpGet("{imdbId}/torrent_mappings")]
    public async Task GetTorrentMappings([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(await Application.instance.GetSeriesTorrentMappingsAsync(imdbId));
    }

    [HttpPost("{imdbId}/torrent_mappings")]
    public async Task SetTorrentMappings([FromRoute] string imdbId, [FromBody] SetTorrentMappingsRequest request)
    {
        List<Application.SetSeriesTorrentMappingsAsyncTorrent> torrents = new();
        foreach(var torrent in request.Torrents)
        {
            int torrentId = await Application.instance.GetTorrentIdByUrlAsync(torrent.Url);
            Application.SetSeriesTorrentMappingsAsyncTorrent? appTorrent = torrents.FirstOrDefault(t => t.Id == torrentId);
            if(appTorrent == null)
            {
                torrents.Add(appTorrent = new(torrentId, []));
            }
            foreach(var mapping in torrent.Mappings)
            {
                int torrentFileId = await Application.instance.GetTorrentFileIdByTorrentIdAndPathAsync(torrentId, mapping.Path);
                int episodeId = await Application.instance.GetSeriesEpisodeIdByImdbIdAsync(mapping.ImdbId);
                appTorrent.Mappings.Add(new(torrentFileId, episodeId));
            }
        }
        await Application.instance.SetSeriesTorrentMappingsAsync(imdbId, torrents);
        await Response.WriteAsJsonAsync(new { message = $"sucessfully set torrent mappings for {imdbId}"});
    }

    [HttpPost("{imdbId}/storage_locations")]
    public async Task SetStorageLocations([FromRoute] string imdbId, [FromBody] SetStorageLocationsRequest request)
    {
        await Application.instance.SetSeriesStorageLocationsAsync(imdbId, request.StorageLocations);
        await Response.WriteAsJsonAsync(new { message = $"series with imdbid {imdbId} has new storage locations"});
    }

    [HttpGet("{imdbId}/storage_locations")]
    public async Task GetStorageLocations([FromRoute] string imdbId)
    {
        await Response.WriteAsJsonAsync(new { StorageLocations = await Application.instance.GetSeriesStorageLocationsAsync(imdbId)});
    }
}