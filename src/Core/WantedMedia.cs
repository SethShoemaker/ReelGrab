using ReelGrab.Database;
using ReelGrab.MediaIndexes;
using ReelGrab.TorrentClients;
using ReelGrab.TorrentClients.Exceptions;
using SqlKata.Execution;
using SqlKata.Extensions;

namespace ReelGrab.Core;

public partial class Application
{
    public async Task<bool> WantedMediaExistsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("WantedMedia").Where("ImdbId", imdbId).FirstOrDefaultAsync()) != null;
    }

    public async Task AddWantedMediaAsync(string imdbId)
    {
        if (await WantedMediaExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} is already wanted");
        }
        MediaType type = await MediaIndex.instance.GetMediaTypeByImdbIdAsync(imdbId);
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
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        MovieDetails details = await MediaIndex.instance.GetMovieDetailsByImdbIdAsync(imdbId);
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
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        SeriesDetails details = await MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId);
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
        using var db = Db.CreateConnection();
        var row = await db
            .Query("WantedMedia")
            .Where("ImdbId", imdbId)
            .Select(["ImdbId", "DisplayName", "Description", "PosterUrl", "Type", "StartYear", "EndYear"])
            .FirstOrDefaultAsync<GetWantedMediaDetailsAsyncWantedMediaRow>()
            ?? throw new WantedMediaDoesNotExistException(imdbId);
        return new(row.ImdbId, row.DisplayName, row.Description, row.PosterUrl, Enum.Parse<MediaType>(row.Type), (int)row.StartYear, (int?)row.EndYear);
    }

    public enum WantedMediaDownloadableType
    {
        SERIES_EPISODE,
        FULL_MOVIE
    }

    private record GetWantedMediaDownloadableTypeAsyncWantedMediaDownloadableRow(string Type);

    public async Task<WantedMediaDownloadableType> GetWantedMediaDownloadableTypeAsync(string wantedMediaDownloadableMediaId)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("WantedMediaDownloadable")
            .Where("ImdbId", wantedMediaDownloadableMediaId)
            .Select("Type")
            .FirstOrDefaultAsync<GetWantedMediaDownloadableTypeAsyncWantedMediaDownloadableRow>();
        if(row.Type == "SeriesEpisode")
        {
            return WantedMediaDownloadableType.SERIES_EPISODE;
        }
        if(row.Type == "FullMovie")
        {
            return WantedMediaDownloadableType.FULL_MOVIE;
        }
        throw new Exception($"unhandled wanted media downloadable type {row.Type}");
    }

    public record WantedMediaDownloadableMetadata(string MediaId, string MediaDisplayName, string DownloadableDisplayName, int DownloadableSeason, int DownloadableEpisode);

    private record GetWantedMediaDownloadableMetadataAsyncRow(string ImdbId, string WantedMediaDisplayName, string DisplayName, long Season, long Episode);

    public async Task<WantedMediaDownloadableMetadata> GetWantedMediaDownloadableMetadataAsync(string wantedMediaDownloadableId)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("WantedMediaDownloadable")
            .Join("WantedMedia", j => j
                .On("WantedMediaDownloadable.MediaId", "WantedMedia.ImdbId"))
            .Where("WantedMediaDownloadable.ImdbId", wantedMediaDownloadableId)
            .Select(["WantedMedia.ImdbId", "WantedMedia.DisplayName as WantedMediaDisplayName", "WantedMediaDownloadable.DisplayName", "WantedMediaDownloadable.Season", "WantedMediaDownloadable.Episode"])
            .FirstOrDefaultAsync<GetWantedMediaDownloadableMetadataAsyncRow>();
        return new(row.ImdbId, row.WantedMediaDisplayName, row.DisplayName, (int)row.Season, (int)row.Episode);
    }

    public record GetAllWantedMediaInProgressAsyncResult(List<GetAllWantedMediaInProgressAsyncResultMedia> Media);

    public record GetAllWantedMediaInProgressAsyncResultMedia(string ImdbId, string DisplayName, MediaType MediaType, List<string> StorageLocations, List<GetAllWantedMediaInProgressAsyncResultMediaDownloadable> Downloadables);

    public record GetAllWantedMediaInProgressAsyncResultMediaDownloadable(string ImdbId, string DisplayName, int Season, int Episode, int Progress);

    private record GetAllWantedMediaInProgressAsyncDbRow(string DownloadableId, string DownloadableDisplayName, long Season, long Episode, string MediaId, string MediaDisplayName, string MediaType, string Hash, string TorrentFilePath, string StorageLocations);

    public async Task<GetAllWantedMediaInProgressAsyncResult> GetAllWantedMediaInProgressAsync()
    {
        using var db = Db.CreateConnection();

        bool none = (await db
            .Query("WantedMediaDownloadable")
            .CountAsync<int>()) == 0;

        if(none)
        {
            return new([]);
        }

        var rows = await db
            .Query("WantedMediaTorrentDownloadable")
            .Join("WantedMediaDownloadable", j => j
                .On("WantedMediaTorrentDownloadable.DownloadableId", "WantedMediaDownloadable.ImdbId")
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaDownloadable.MediaId"))
            .Join("WantedMedia", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMedia.ImdbId"))
            .LeftJoin("WantedMediaStorageLocation", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaStorageLocation.MediaId"))
            .LeftJoin("WantedMediaTorrent", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaTorrent.MediaId")
                .On("WantedMediaTorrentDownloadable.TorrentDisplayName", "WantedMediaTorrent.DisplayName"))
            .OrderBy("WantedMediaDownloadable.Season")
            .OrderBy("WantedMediaDownloadable.Episode")
            .GroupBy(["WantedMediaTorrentDownloadable.DownloadableId", "WantedMediaDownloadable.DisplayName", "WantedMediaDownloadable.Season", "WantedMediaDownloadable.Episode", "WantedMediaTorrentDownloadable.MediaId", "WantedMedia.DisplayName", "WantedMedia.Type", "WantedMediaTorrent.Hash", "WantedMediaTorrentDownloadable.TorrentFilePath"])
            .SelectRaw("WantedMediaTorrentDownloadable.DownloadableId, WantedMediaDownloadable.DisplayName as DownloadableDisplayName, WantedMediaDownloadable.Season, WantedMediaDownloadable.Episode, WantedMediaTorrentDownloadable.MediaId, WantedMedia.DisplayName as MediaDisplayName, WantedMedia.Type as MediaType, WantedMediaTorrent.Hash, WantedMediaTorrentDownloadable.TorrentFilePath, GROUP_CONCAT(WantedMediaStorageLocation.StorageLocation, ',') as StorageLocations")
            .GetAsync<GetAllWantedMediaInProgressAsyncDbRow>();

        GetAllWantedMediaInProgressAsyncResult res = new([]);

        Dictionary<string, List<ITorrentClient.TorrentFileInfo>?> torrentFiles = new();

        foreach (var row in rows)
        {
            GetAllWantedMediaInProgressAsyncResultMedia? media = res.Media.FirstOrDefault(m => m.ImdbId == row.MediaId);
            if (media == null)
            {
                media = new(row.MediaId, row.MediaDisplayName, Enum.Parse<MediaType>(row.MediaType), row.StorageLocations.Split(',').ToList(), []);
                res.Media.Add(media);
            }
            if (!torrentFiles.TryGetValue(row.Hash, out List<ITorrentClient.TorrentFileInfo>? files))
            {
                try
                {
                    files = await TorrentClient.instance.GetTorrentFilesByHashAsync(row.Hash);
                }
                catch (TorrentDoesNotExistException)
                {
                    files = null;
                }
                torrentFiles[row.Hash] = files;
            }
            int progress = (files?.FirstOrDefault(f => f.Path == row.TorrentFilePath)?.Progress) ?? 0;
            media.Downloadables.Add(new(row.DownloadableId, row.DownloadableDisplayName, (int)row.Season, (int)row.Episode, progress));
        }
        return res;
    }

    public record WantedSeriesSeason(int Number, List<WantedSeriesEpisode> Episodes);

    public record WantedSeriesEpisode(int Number, string Title, string ImdbId, bool Wanted);

    private record GetWantedSeriesEpisodesAsyncWantedMediaDownloadableRow(string ImdbId, string DisplayName, long Wanted, long Season, long Episode);

    public async Task<List<WantedSeriesSeason>> GetWantedSeriesEpisodesAsync(string seriesImdbId)
    {
        using var db = Db.CreateConnection();
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
        using var db = Db.CreateConnection();
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
        using var db = Db.CreateConnection();
        List<object[]> rows = new();
        SeriesDetails details = await MediaIndex.instance.GetSeriesDetailsByImdbIdAsync(imdbId);
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

    public record MediaTorrent(string TorrentUrl, string Source, string DisplayName, string FilePath);

    public async Task<MediaTorrent?> GetWantedMovieTorrentAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedMovieExistsAsync(imdbId, db);
        var torrent = await db
            .Query("WantedMediaTorrentDownloadable")
            .Select(["WantedMediaTorrent.TorrentUrl", "WantedMediaTorrent.Source", "WantedMediaTorrent.DisplayName", "WantedMediaTorrentDownloadable.TorrentFilePath"])
            .Where("DownloadableId", imdbId)
            .Join("WantedMediaTorrent", j => j
                .On("WantedMediaTorrentDownloadable.TorrentDisplayName", "WantedMediaTorrent.DisplayName")
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaTorrent.MediaId")
            )
            .FirstOrDefaultAsync();
        if (torrent == null)
        {
            return null;
        }
        return new(torrent.TorrentUrl, torrent.Source, torrent.DisplayName, torrent.TorrentFilePath);
    }

    public async Task SetWantedMovieTorrentAsync(string movieImdbId, MediaTorrent mediaTorrent)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedMovieExistsAsync(movieImdbId, db);
        string hash = mediaTorrent.TorrentUrl.StartsWith("magnet") ? await Utils.Torrents.GetTorrentHashByMagnetLinkAsync(mediaTorrent.TorrentUrl) : await Utils.Torrents.GetTorrentHashByUrlAsync(mediaTorrent.TorrentUrl);
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("WantedMediaTorrentDownloadable")
            .Where("MediaId", movieImdbId)
            .DeleteAsync();
        await db
            .Query("WantedMediaTorrent")
            .Where("MediaId", movieImdbId)
            .DeleteAsync();
        await db
            .Query("WantedMediaTorrent")
            .InsertAsync(new
            {
                MediaId = movieImdbId,
                TorrentUrl = mediaTorrent.TorrentUrl.Replace(" ", "%20"),
                mediaTorrent.Source,
                mediaTorrent.DisplayName,
                Hash = hash
            });
        await db
            .Query("WantedMediaTorrentDownloadable")
            .InsertAsync(new
            {
                MediaId = movieImdbId,
                TorrentDisplayName = mediaTorrent.DisplayName,
                TorrentFilePath = mediaTorrent.FilePath,
                DownloadableId = movieImdbId,
            });
        transaction.Commit();
    }

    public record GetWantedSeriesTorrentsAsyncSeason(int Number, List<GetWantedSeriesTorrentsAsyncEpisode> Episodes);

    public record GetWantedSeriesTorrentsAsyncEpisode(int Number, string Title, string ImdbId, bool Wanted, MediaTorrent? MediaTorrent);

    private record GetWantedSeriesTorrentsAsyncRow(string ImdbId, string DisplayName, long Wanted, long Season, long Episode, string? TorrentUrl, string? Source, string? TorrentDisplayName, string? TorrentFilePath);

    public async Task<List<GetWantedSeriesTorrentsAsyncSeason>> GetWantedSeriesTorrentsAsync(string seriesImdbId)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedSeriesExistsAsync(seriesImdbId, db);
        var downloadables = await db
            .Query("WantedMediaDownloadable")
            .Where("WantedMediaDownloadable.MediaId", seriesImdbId)
            .LeftJoin("WantedMediaTorrentDownloadable", j => j
                .On("WantedMediaDownloadable.ImdbId", "WantedMediaTorrentDownloadable.DownloadableId"))
            .LeftJoin("WantedMediaTorrent", j => j
                .On("WantedMediaTorrentDownloadable.MediaId", "WantedMediaTorrent.MediaId")
                .On("WantedMediaTorrentDownloadable.TorrentDisplayName", "WantedMediaTorrent.DisplayName"))
            .Select([
                "WantedMediaDownloadable.ImdbId",
                "WantedMediaDownloadable.DisplayName",
                "WantedMediaDownloadable.Wanted",
                "WantedMediaDownloadable.Season",
                "WantedMediaDownloadable.Episode",
                "WantedMediaTorrent.TorrentUrl",
                "WantedMediaTorrent.Source",
                "WantedMediaTorrent.DisplayName as TorrentDisplayName",
                "WantedMediaTorrentDownloadable.TorrentFilePath"])
            .GetAsync<GetWantedSeriesTorrentsAsyncRow>();

        List<GetWantedSeriesTorrentsAsyncSeason> seasons = new();
        foreach (var downloadable in downloadables)
        {
            GetWantedSeriesTorrentsAsyncSeason? season = seasons.FirstOrDefault(s => s.Number == downloadable.Season);
            if(season == null)
            {
                season = new GetWantedSeriesTorrentsAsyncSeason((int)downloadable.Season, []);
                seasons.Add(season);
            }
            MediaTorrent? mediaTorrent = downloadable.TorrentUrl == null ? null : new(downloadable.TorrentUrl, downloadable.Source!, downloadable.TorrentDisplayName!, downloadable.TorrentFilePath!);
            season.Episodes.Add(new((int)downloadable.Episode, downloadable.DisplayName, downloadable.ImdbId, downloadable.Wanted == 1, mediaTorrent));
        }
        return seasons;
    }

    public record WantedSeriesTorrentEpisodeDto(string ImdbId, string TorrentFilePath);

    public record WantedSeriesTorrentDto(string TorrentUrl, string TorrentSource, string TorrentDisplayName, List<WantedSeriesTorrentEpisodeDto> Episodes);

    public async Task SetWantedSeriesTorrentsAsync(string seriesImdbId, List<WantedSeriesTorrentDto> torrents)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedSeriesExistsAsync(seriesImdbId, db);
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("WantedMediaTorrentDownloadable")
            .Where("MediaId", seriesImdbId)
            .DeleteAsync();
        await db
            .Query("WantedMediaTorrent")
            .Where("MediaId", seriesImdbId)
            .DeleteAsync();
        string[] torrentCols = ["MediaId", "TorrentUrl", "Source", "DisplayName", "Hash"];
        Dictionary<string, string> urlToHashMap = new();
        foreach (var torrent in torrents)
        {
            if (!urlToHashMap.TryGetValue(torrent.TorrentUrl, out string? hash))
            {
                hash = torrent.TorrentUrl.StartsWith("magnet") ? await Utils.Torrents.GetTorrentHashByMagnetLinkAsync(torrent.TorrentUrl) : await Utils.Torrents.GetTorrentHashByUrlAsync(torrent.TorrentUrl);
                urlToHashMap[torrent.TorrentUrl] = hash;
            }
        }
        var torrentRows = torrents.Select(t => new object[] {
            seriesImdbId,
            t.TorrentUrl.Replace(" ", "%20"),
            t.TorrentSource,
            t.TorrentDisplayName,
            urlToHashMap[t.TorrentUrl]
        });
        await db
            .Query("WantedMediaTorrent")
            .InsertAsync(torrentCols, torrentRows);
        string[] torrentDownloadableCols = ["MediaId", "TorrentDisplayName", "TorrentFilePath", "DownloadableId"];
        var torrentDownloadableRows = torrents.SelectMany(t => t.Episodes, (torrent, episode) => new object[] {
            seriesImdbId,
            torrent.TorrentDisplayName,
            episode.TorrentFilePath,
            episode.ImdbId
        });
        await db
            .Query("WantedMediaTorrentDownloadable")
            .InsertAsync(torrentDownloadableCols, torrentDownloadableRows);
        transaction.Commit();
    }

    private record GetWantedMediaStorageLocationsAsyncWantedMediaStorageLocationRow(string StorageLocation);

    public async Task<List<string>> GetWantedMediaStorageLocationsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedMediaExistsAsync(imdbId, db);
        return (await db
            .Query("WantedMediaStorageLocation")
            .Where("MediaId", imdbId)
            .Select(["StorageLocation"])
            .GetAsync<GetWantedMediaStorageLocationsAsyncWantedMediaStorageLocationRow>())
            .Select(r => r.StorageLocation)
            .ToList();
    }

    public async Task SetWantedMediaStorageLocationsAsync(string imdbId, List<string> storageLocations)
    {
        using var db = Db.CreateConnection();
        await EnsureWantedMediaExistsAsync(imdbId, db);
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("WantedMediaStorageLocation")
            .Where("MediaId", imdbId)
            .DeleteAsync();
        await db
            .Query("WantedMediaStorageLocation")
            .InsertAsync(
                ["MediaId", "StorageLocation"],
                storageLocations.Select(sl => new object[] { imdbId, sl })
            );
        transaction.Commit();
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

    private async Task EnsureWantedMediaExistsAsync(string imdbId, QueryFactory db)
    {
        var row = await db
            .Query("WantedMedia")
            .Where("ImdbId", imdbId)
            .FirstOrDefaultAsync();
        if (row == null)
        {
            throw new WantedMediaDoesNotExistException(imdbId);
        }
    }

    public class WantedMediaIsNotAMovieException : InvalidOperationException
    {
        public string MovieImdbId;

        public WantedMediaIsNotAMovieException(string movieImdbId)
        {
            MovieImdbId = movieImdbId;
        }

        public override string Message => $"{MovieImdbId} is not a movie";
    }

    private async Task EnsureWantedMovieExistsAsync(string movieImdbId, QueryFactory db)
    {
        string type = await db
            .Query("WantedMedia")
            .Where("ImdbId", movieImdbId)
            .Select("Type")
            .FirstOrDefaultAsync<string>()
            ?? throw new WantedMediaDoesNotExistException(movieImdbId);
        if (type != MediaType.MOVIE.ToString())
        {
            throw new WantedMediaIsNotAMovieException(movieImdbId);
        }
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