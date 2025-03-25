using ReelGrab.Database;
using ReelGrab.MediaIndexes;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public async Task<bool> SeriesWithImdbIdExistsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("Series").Where("ImdbId", imdbId).CountAsync<int>()) > 0;
    }

    public async Task<bool> SeriesWithIdExistsAsync(int id)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("Series").Where("Id", id).CountAsync<int>()) > 0;
    }

    public async Task AddSeriesByImdbIdAsync(string imdbId)
    {
        if (await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} already exists");
        }
        MediaType type = await MediaIndex.instance.GetMediaTypeByImdbIdAsync(imdbId);
        if (type != MediaType.SERIES)
        {
            throw new Exception($"{imdbId} is of type {type}, not series");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        try
        {
            SeriesDetails details = await MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId);
            await db.Query("Series").InsertAsync(new
            {
                ImdbId = imdbId,
                Name = details.Title,
                Description = details.Plot,
                Poster = details.PosterUrl,
                StartYear = details.StartYear,
                EndYear = details.EndYear
            });
            int seriesId = await db
                .Query("Series")
                .Where("ImdbId", imdbId)
                .Select("Id")
                .FirstOrDefaultAsync<int>();
            foreach (var season in details.Seasons)
            {
                await db.Query("SeriesSeason").InsertAsync(new
                {
                    SeriesId = seriesId,
                    Number = season.Number
                });
                int seasonId = await db
                    .Query("SeriesSeason")
                    .Where("SeriesId", seriesId)
                    .Where("Number", season.Number)
                    .Select("Id")
                    .FirstOrDefaultAsync<int>();
                await db.Query("SeriesEpisode").InsertAsync(
                    ["SeasonId", "Number", "ImdbId", "Name", "Wanted"],
                    season.Episodes.Select(e => new object[] {
                        seasonId,
                        e.Number,
                        e.ImdbId,
                        e.Title,
                        0
                    })
                );
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task SetSeriesEpisodesWantedAsync(List<string> episodeImdbIds, bool wanted = true)
    {
        using var db = Db.CreateConnection();
        await db
            .Query("SeriesEpisode")
            .WhereIn("ImdbId", episodeImdbIds)
            .UpdateAsync(new
            {
                Wanted = wanted ? 1 : 0
            });
    }
}