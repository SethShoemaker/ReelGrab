using System.Text.RegularExpressions;
using ReelGrab.Core;

namespace ReelGrab.Web.Routers;

public partial class MediaWantedRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/wanted_media";

        app.MapGet($"{baseUrl}/check_wanted", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(new { Wanted = await Application.instance.WantedMediaExistsAsync(imdbId) });
        });

        app.MapPost($"{baseUrl}", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            if (await Application.instance.WantedMediaExistsAsync(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = $"{imdbId} already is wanted" });
                return;
            }
            await Application.instance.AddWantedMediaAsync(imdbId);
            await context.Response.WriteAsJsonAsync(new { message = $"{imdbId} is now wanted" });
        });

        app.MapGet($"{baseUrl}/details", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(await Application.instance.GetWantedMediaDetailsAsync(imdbId));
        });

        app.MapGet($"{baseUrl}/series_episodes", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(new { seasons = await Application.instance.GetWantedSeriesEpisodesAsync(imdbId) });
        });

        app.MapPost($"{baseUrl}/series_episodes", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            string? episodes = context.Request.Query["episodes"];
            if (episodes == null)
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide episodes" });
                return;
            }
            List<Application.WantedSeriesEpisodeDto> episodeDtos = new();
            if (!string.IsNullOrWhiteSpace(episodes))
            {
                foreach (string episode in episodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    Match match = ValidateSeriesEpisode().Match(episode);
                    if (!match.Success)
                    {
                        await context.Response.WriteAsJsonAsync(new { message = $"{episode} is not formatted correctly" });
                        return;
                    }
                    int seasonNumber = int.Parse(match.Groups[1].Value);
                    int episodeNumber = int.Parse(match.Groups[2].Value);
                    episodeDtos.Add(new(seasonNumber, episodeNumber));
                }
            }
            await Application.instance.SetWantedSeriesEpisodesAsync(imdbId, episodeDtos);
            await context.Response.WriteAsJsonAsync(new { message = "successfully set new wanted episodes", imdbId = imdbId });
        });

        app.MapGet($"{baseUrl}/refresh_series_episodes", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if(string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await Application.instance.RefreshWantedSeriesEpisodes(imdbId);
            await context.Response.WriteAsJsonAsync(new { message = "successfully refreshed series episodes" });
        });

        app.MapGet($"{baseUrl}/movie_torrent", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(new { torrent = await Application.instance.GetWantedMovieTorrentAsync(imdbId) });
        });

        app.MapPost($"{baseUrl}/movie_torrent", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            string? torrentUrl = context.Request.Query["torrentUrl"];
            if (string.IsNullOrWhiteSpace(torrentUrl))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide torrentUrl" });
                return;
            }
            string? torrentSource = context.Request.Query["torrentSource"];
            if (string.IsNullOrWhiteSpace(torrentSource))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide torrentSource" });
                return;
            }
            string? torrentDisplayName = context.Request.Query["torrentDisplayName"];
            if (string.IsNullOrWhiteSpace(torrentDisplayName))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide torrentDisplayName" });
                return;
            }
            string? torrentFilePath = context.Request.Query["torrentFilePath"];
            if (string.IsNullOrWhiteSpace(torrentFilePath))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide torrentFilePath" });
                return;
            }
            await Application.instance.SetWantedMovieTorrentAsync(imdbId, new(torrentUrl, torrentSource, torrentDisplayName, torrentFilePath));
            await context.Response.WriteAsJsonAsync(new { message = "successfully set movie torrent" });
        });

        app.MapGet($"{baseUrl}/series_torrents", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(await Application.instance.GetWantedSeriesTorrentsAsync(imdbId));
        });

        app.MapPost($"{baseUrl}/series_torrents", async context => {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            var torrents = await context.Request.ReadFromJsonAsync<List<Application.WantedSeriesTorrentDto>>() ?? throw new Exception("you fucked up");
            await Application.instance.SetWantedSeriesTorrentsAsync(imdbId, torrents);
            await context.Response.WriteAsJsonAsync(new { message = "successfully set series torrents" });
        });

        app.MapGet($"{baseUrl}/storage_locations", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            await context.Response.WriteAsJsonAsync(new { storageLocations = await Application.instance.GetWantedMediaStorageLocationsAsync(imdbId) });
        });

        app.MapPost($"{baseUrl}/storage_locations", async context =>
        {
            string? imdbId = context.Request.Query["imdbId"];
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide imdbId" });
                return;
            }
            string? storageLocations = context.Request.Query["storageLocations"];
            if (string.IsNullOrWhiteSpace(storageLocations))
            {
                await context.Response.WriteAsJsonAsync(new { message = "did not provide storageLocations" });
                return;
            }
            List<string> locations = storageLocations.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            await Application.instance.SetWantedMediaStorageLocationsAsync(imdbId, locations);
            await context.Response.WriteAsJsonAsync(new { message = "successfully set new storage locations" });
        });

        app.MapGet($"{baseUrl}/in_progress", async context => {
            await context.Response.WriteAsJsonAsync(await Application.instance.GetAllWantedMediaInProgressAsync());
        });
    }

    [GeneratedRegex("^S(\\d{2})E(\\d{2})$")]
    private static partial Regex ValidateSeriesEpisode();
}