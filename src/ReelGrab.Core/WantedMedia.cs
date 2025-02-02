using ReelGrab.Media;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public async Task<bool> WantedMediaExistsAsync(string imdbId)
    {
        using var db = Db();
        return (await db.Query("WantedMedia").Where("ImdbId", imdbId).FirstOrDefaultAsync()) != null;
    }

    public async Task AddWantedMediaAsync(string imdbId)
    {
        if (await WantedMediaExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} is already wanted");
        }
        MediaType type = await mediaIndex.GetMediaTypeByImdbIdAsync(imdbId);
        switch (type)
        {
            case MediaType.MOVIE:
                await AddWantedMovieAsync(imdbId);
                break;
            case MediaType.SERIES:
                await AddWantedSeriesAsync(imdbId);
                break;
            default:
                throw new NotImplementedException($"unhandled media type ${type}");
        }
    }

    public async Task AddWantedMovieAsync(string imdbId)
    {
        using var db = Db();
        using var transaction = db.Connection.BeginTransaction();
        MovieDetails details = await mediaIndex.GetMovieDetailsByImdbIdAsync(imdbId);
        await db.Query("WantedMedia").InsertAsync(new
        {
            ImdbId = imdbId,
            DisplayName = details.Title,
            Type = MediaType.MOVIE.ToString(),
            StartYear = details.Year,
            EndYear = details.Year,
            PosterUrl = details.PosterUrl,
            Description = details.Plot
        });
        await db.Query("WantedMediaDownloadable").InsertAsync(new
        {
            MediaId = imdbId,
            ImdbId = imdbId,
            DisplayName = details.Title,
            Wanted = 1,
            Type = "FullMovie",
            Season = 1,
            Episode = 1
        });
        transaction.Commit();
    }

    public async Task AddWantedSeriesAsync(string imdbId)
    {
        using var db = Db();
        using var transaction = db.Connection.BeginTransaction();
        SeriesDetails details = await mediaIndex.GetSeriesDetailsByImdbIdAsync(imdbId);
        await db.Query("WantedMedia").InsertAsync(new
        {
            ImdbId = imdbId,
            DisplayName = details.Title,
            Type = MediaType.SERIES.ToString(),
            details.StartYear,
            details.EndYear,
            PosterUrl = details.PosterUrl,
            Description = details.Plot
        });
        string[] cols = ["MediaId", "ImdbId", "DisplayName", "Wanted", "Type", "Season", "Episode"];
        var rows = details.Seasons.SelectMany(season => season.Episodes, (season, episode) => new object[] {
            imdbId,
            episode.ImdbId,
            episode.Title,
            0,
            "SeriesEpisode",
            season.Number,
            episode.Number
        });
        await db.Query("WantedMediaDownloadable").InsertAsync(cols, rows);
        transaction.Commit();
    }

    public record WantedMediaDetails(string ImdbId, string Title, string Description, string PosterUrl, MediaType MediaType, int StartYear, int? EndYear);

    private record GetWantedMediaDetailsAsyncWantedMediaRow(string ImdbId, string DisplayName, string Description, string PosterUrl, string Type, long StartYear, long? EndYear);

    public async Task<WantedMediaDetails> GetWantedMediaDetailsAsync(string imdbId)
    {
        using var db = Db();
        var row = await db
            .Query("WantedMedia")
            .Where("ImdbId", imdbId)
            .Select(["ImdbId", "DisplayName", "Description", "PosterUrl", "Type", "StartYear", "EndYear"])
            .FirstOrDefaultAsync<GetWantedMediaDetailsAsyncWantedMediaRow>()
            ?? throw new WantedMediaDoesNotExistException(imdbId);
        return new(row.ImdbId, row.DisplayName, row.Description, row.PosterUrl, Enum.Parse<MediaType>(row.Type), (int)row.StartYear, (int?)row.EndYear);
    }

    public record WantedSeriesSeason(int Number, List<WantedSeriesEpisode> Episodes);

    public record WantedSeriesEpisode(int Number, string Title, string ImdbId, bool Wanted);

    private record GetWantedSeriesEpisodesAsyncWantedMediaDownloadableRow(string ImdbId, string DisplayName, long Wanted, long Season, long Episode);

    public async Task<List<WantedSeriesSeason>> GetWantedSeriesEpisodesAsync(string seriesImdbId)
    {
        using var db = Db();
        await EnsureWantedSeriesExistsAsync(seriesImdbId, db);
        var rows = await db
            .Query("WantedMediaDownloadable")
            .Where("MediaId", seriesImdbId)
            .Select("ImdbId", "DisplayName", "Wanted", "Season", "Episode")
            .GetAsync<GetWantedSeriesEpisodesAsyncWantedMediaDownloadableRow>();
        List<WantedSeriesSeason> seasons = new();
        foreach (var row in rows)
        {
            WantedSeriesSeason? season = seasons.FirstOrDefault(s => s.Number == row.Season);
            if (season == null)
            {
                season = new WantedSeriesSeason((int)row.Season, []);
                seasons.Add(season);
            }
            season.Episodes.Add(new((int)row.Episode, row.DisplayName, row.ImdbId, row.Wanted == 1));
        }
        return seasons;
    }

    public record WantedSeriesEpisodeDto(int Season, int Episode);

    public async Task SetWantedSeriesEpisodesAsync(string seriesImdbId, List<WantedSeriesEpisodeDto> wantedEpisodes)
    {
        using var db = Db();
        using var transaction = db.Connection.BeginTransaction();
        var row = await db.Query("WantedMedia").Where("ImdbId", seriesImdbId).Select("Type", "EndYear").FirstOrDefaultAsync() ?? throw new WantedMediaDoesNotExistException(seriesImdbId);
        if (row.Type != MediaType.SERIES.ToString())
        {
            throw new InvalidOperationException($"{seriesImdbId} is not a series, cannot set wanted series episodes");
        }
        if (row.EndYear == null)
        {
            await RefreshWantedSeriesEpisodes(seriesImdbId);
        }
        await db
            .Query("WantedMediaDownloadable")
            .Where("MediaId", seriesImdbId)
            .UpdateAsync(new
            {
                Wanted = 0
            });
        foreach (WantedSeriesEpisodeDto episode in wantedEpisodes)
        {
            await EnsureWantedMediaSeriesEpisodeExistsAsync(seriesImdbId, episode.Season, episode.Episode, db);
            await db
                .Query("WantedMediaDownloadable")
                .Where("MediaId", seriesImdbId)
                .Where("Season", episode.Season)
                .Where("Episode", episode.Episode)
                .UpdateAsync(new
                {
                    Wanted = 1
                });
        }
        transaction.Commit();
    }

    public async Task RefreshWantedSeriesEpisodes(string imdbId)
    {
        using var db = Db();
        List<object[]> rows = new();
        SeriesDetails details = await mediaIndex.GetSeriesDetailsByImdbIdAsync(imdbId);
        foreach (var season in details.Seasons)
        {
            foreach (var episode in season.Episodes)
            {
                try
                {
                    await EnsureWantedMediaSeriesEpisodeExistsAsync(imdbId, season.Number, episode.Number, db);
                }
                catch (WantedSeriesEpisodeDoesNotExistException)
                {
                    rows.Add([imdbId, episode.ImdbId, episode.Title, 0, "SeriesEpisode", season.Number, episode.Number]);
                }
            }
        }
        if (rows.Count != 0)
        {
            string[] columns = ["MediaId", "ImdbId", "DisplayName", "Wanted", "Type", "Season", "Episode"];
            await db.Query("WantedMediaDownloadable").InsertAsync(columns, rows);
        }
    }

    public class WantedMediaDoesNotExistException : Exception
    {
        public string ImdbId;

        public WantedMediaDoesNotExistException(string imdbid)
        {
            ImdbId = imdbid;
        }

        public override string Message => $"{ImdbId} does not exist";
    }

    public class WantedMediaIsNotASeriesException : InvalidOperationException
    {
        public string SeriesImdbId;

        public WantedMediaIsNotASeriesException(string seriesImdbId)
        {
            SeriesImdbId = seriesImdbId;
        }

        public override string Message => $"{SeriesImdbId} is not a series";
    }

    private async Task EnsureWantedSeriesExistsAsync(string seriesImdbId, QueryFactory db)
    {
        string type = await db
            .Query("WantedMedia")
            .Where("ImdbId", seriesImdbId)
            .Select("Type")
            .FirstOrDefaultAsync<string>()
            ?? throw new WantedMediaDoesNotExistException(seriesImdbId);
        if (type != MediaType.SERIES.ToString())
        {
            throw new WantedMediaIsNotASeriesException(seriesImdbId);
        }
    }

    public class WantedSeriesEpisodeDoesNotExistException : Exception
    {
        public string SeriesImdbId;

        public int Season;

        public int Episode;

        public WantedSeriesEpisodeDoesNotExistException(string seriesImdbId, int season, int episode)
        {
            SeriesImdbId = seriesImdbId;
            Season = season;
            Episode = episode;
        }

        public override string Message => $"{SeriesImdbId} does not have a record for season {Season} episode {Episode}";
    }

    private async Task EnsureWantedMediaSeriesEpisodeExistsAsync(string seriesImdbId, int season, int episode, QueryFactory db)
    {
        int count = (await db
            .Query("WantedMediaDownloadable")
            .Where("MediaId", seriesImdbId)
            .Where("Season", season)
            .Where("Episode", episode)
            .AsCount()
            .GetAsync<int>()
            ).First();
        if (count != 1)
        {
            throw new WantedSeriesEpisodeDoesNotExistException(seriesImdbId, season, episode);
        }
    }
}