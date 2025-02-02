using System.Text.RegularExpressions;
using ReelGrab.Core;

namespace ReelGrab.Web.Routers;

public partial class MediaWantedRouter : Router
{
    public override void Route(WebApplication app)
    {
        string baseUrl = "/wanted_media";

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
    }

    [GeneratedRegex("^S(\\d{2})E(\\d{2})$")]
    private static partial Regex ValidateSeriesEpisode();
}