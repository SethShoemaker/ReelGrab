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

    public record AddSeriesAsyncEpisode(int Number, string Name, string ImdbId, string? Description, string? Poster, bool Wanted);

    public record AddSeriesAsyncSeason(int Number, string? Description, string? Poster, List<AddSeriesAsyncEpisode> Episodes);

    public async Task AddSeriesAsync(string imdbId, string name, string? description, string? poster, int startYear, int? endYear, List<AddSeriesAsyncSeason> seasons)
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
            int seriesId = await db.Query("Series").InsertGetIdAsync<int>(new
            {
                ImdbId = imdbId,
                Name = name,
                Description = description,
                Poster = poster,
                StartYear = startYear,
                EndYear = endYear
            });
            foreach (var season in seasons)
            {
                int seasonId = await db.Query("SeriesSeason").InsertGetIdAsync<int>(new
                {
                    SeriesId = seriesId,
                    Number = season.Number,
                    Description = season.Description,
                    Poster = season.Poster
                });
                await db.Query("SeriesEpisode").InsertAsync(
                    ["SeasonId", "Number", "ImdbId", "Name", "Wanted"],
                    season.Episodes.Select(e => new object[] {
                        seasonId,
                        e.Number,
                        e.ImdbId,
                        e.Name,
                        e.Wanted ? 1 : 0
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

    public record GetSeriesEpisodesAsyncResultEpisode(int Id, int Number, string ImdbId, string Name, bool Wanted);

    public record GetSeriesEpisodesAsyncResultSeason(int Id, int Number, List<GetSeriesEpisodesAsyncResultEpisode> Episodes);

    public record GetSeriesEpisodesAsyncResult(List<GetSeriesEpisodesAsyncResultSeason> Seasons);

    private record GetSeriesEpisodesAsyncRow(long SeasonId, long SeasonNumber, long EpisodeId, long EpisodeNumber, string EpisodeImdbId, string EpisodeName, long Wanted);

    public async Task<GetSeriesEpisodesAsyncResult> GetSeriesEpisodesAsync(string imdbId)
    {
        if (!await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Series")
            .Join("SeriesSeason", j => j.On("Series.Id", "SeriesSeason.SeriesId"))
            .Join("SeriesEpisode", j => j.On("SeriesSeason.Id", "SeriesEpisode.SeasonId"))
            .Where("Series.ImdbId", imdbId)
            .Select(["SeriesSeason.Id", "SeriesSeason.Number", "SeriesEpisode.Id", "SeriesEpisode.Number", "SeriesEpisode.ImdbId", "SeriesEpisode.Name", "SeriesEpisode.Wanted"])
            .GetAsync<GetSeriesEpisodesAsyncRow>();
        List<GetSeriesEpisodesAsyncResultSeason> seasons = new();
        foreach (var row in rows)
        {
            GetSeriesEpisodesAsyncResultSeason? season = seasons.FirstOrDefault(s => s.Id == row.SeasonId);
            if (season == null)
            {
                seasons.Add(season = new((int)row.SeasonId, (int)row.SeasonNumber, []));
            }
            season.Episodes.Add(new((int)row.EpisodeId, (int)row.EpisodeNumber, row.EpisodeImdbId, row.EpisodeName, row.Wanted == 1));
        }
        return new(seasons);
    }

    public record UpdateSeriesEpisodesAsyncEpisode(int Number, string ImdbId, string Name);

    public record UpdateSeriesEpisodesAsyncSeason(int Number, List<UpdateSeriesEpisodesAsyncEpisode> Episodes);

    public async Task UpdateSeriesEpisodesAsync(string imdbId, List<UpdateSeriesEpisodesAsyncSeason> seasons)
    {
        if (!await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        try
        {
            int seriesId = await db
                .Query("Series")
                .Where("ImdbId", imdbId)
                .Select("Id")
                .FirstAsync<int>();
            foreach (var season in seasons)
            {
                int? seasonId = await db
                    .Query("SeriesSeason")
                    .Where("Number", season.Number)
                    .Where("SeriesId", seriesId)
                    .Select("Id")
                    .FirstOrDefaultAsync<int>();
                bool seasonMissing = seasonId == null;
                if (seasonMissing)
                {
                    seasonId = await db
                        .Query("SeriesSeason")
                        .InsertGetIdAsync<int>(new
                        {
                            SeriesId = seriesId,
                            Number = season.Number
                        });
                }
                foreach (var episode in season.Episodes)
                {
                    bool episodeMissing = seasonMissing || (await db
                        .Query("SeriesEpisode")
                        .Where("SeasonId", seasonId)
                        .Where("Number", episode.Number)
                        .CountAsync<int>()) == 0;
                    if (episodeMissing)
                    {
                        await db
                            .Query("SeriesEpisode")
                            .InsertAsync(new
                            {
                                SeasonId = seasonId,
                                Number = episode.Number,
                                ImdbId = episode.ImdbId,
                                Name = episode.Name,
                                Wanted = 0
                            });
                        continue;
                    }
                    bool episodeUpdatable = (await db
                        .Query("SeriesEpisode")
                        .Where("SeasonId", seasonId)
                        .Where("ImdbId", episode.ImdbId)
                        .Where("Number", episode.Number)
                        .Where("Name", episode.Name)
                        .CountAsync<int>()) == 0;
                    if (episodeUpdatable)
                    {
                        await db
                            .Query("SeriesEpisode")
                            .Where("SeasonId", seasonId)
                            .Where("Number", episode.Number)
                            .UpdateAsync(new
                            {
                                Name = episode.Name,
                                ImdbId = episode.ImdbId
                            });
                    }
                }
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

    public record GetSeriesWantedInfoAsyncResultEpisode(int Number, string Name, string ImdbId, bool Wanted);

    public record GetSeriesWantedInfoAsyncResultSeason(int Number, List<GetSeriesWantedInfoAsyncResultEpisode> Episodes);

    public record GetSeriesWantedInfoAsyncResult(List<GetSeriesWantedInfoAsyncResultSeason> Seasons);

    private record GetSeriesWantedInfoAsyncRow(long SeasonNumber, long EpisodeNumber, string EpisodeName, string EpisodeImdbId, long EpisodeWanted);

    public async Task<GetSeriesWantedInfoAsyncResult> GetSeriesWantedInfoAsync(string imdbId)
    {
        if(! await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Series")
            .Join("SeriesSeason", j => j.On("Series.Id", "SeriesSeason.SeriesId"))
            .Join("SeriesEpisode", j => j.On("SeriesSeason.Id", "SeriesEpisode.SeasonId"))
            .Where("Series.ImdbId", imdbId)
            .Select(["SeriesSeason.Number AS SeasonNumber", "SeriesEpisode.Number AS EpisodeNumber", "SeriesEpisode.Name AS EpisodeName", "SeriesEpisode.ImdbId AS EpisodeImdbId", "SeriesEpisode.Wanted AS EpisodeWanted"])
            .GetAsync<GetSeriesWantedInfoAsyncRow>();
        GetSeriesWantedInfoAsyncResult result = new([]);
        foreach(var row in rows)
        {
            GetSeriesWantedInfoAsyncResultSeason? season = result.Seasons.FirstOrDefault(s => s.Number == (int)row.SeasonNumber);
            if(season == null)
            {
                result.Seasons.Add(season = new((int)row.SeasonNumber, []));
            }
            season.Episodes.Add(new((int)row.EpisodeNumber, row.EpisodeName, row.EpisodeImdbId, row.EpisodeWanted == 1));
        }
        return result;
    }

    public record SetSeriesTorrentMappingsAsyncMapping(int TorrentFileId, int EpisodeId);

    public record SetSeriesTorrentMappingsAsyncTorrent(int Id, List<SetSeriesTorrentMappingsAsyncMapping> Mappings);

    public async Task SetSeriesTorrentMappingsAsync(string imdbId, List<SetSeriesTorrentMappingsAsyncTorrent> torrents)
    {
        if(! await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        int seriesId = await db
            .Query("Series")
            .Where("ImdbId", imdbId)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
        var seriesTorrentIds = await db
            .Query("SeriesTorrent")
            .Where("SeriesId", seriesId)
            .Select("Id")
            .GetAsync<int>();
        await db
            .Query("SeriesTorrentMapping")
            .WhereIn("SeriesTorrentId", seriesTorrentIds)
            .DeleteAsync();
        await db
            .Query("SeriesTorrent")
            .Where("SeriesId", seriesId)
            .DeleteAsync();
        foreach(var torrent in torrents)
        {
            var seriesTorrentId = await db
                .Query("SeriesTorrent")
                .InsertGetIdAsync<int>(new {
                    SeriesId = seriesId,
                    TorrentId = torrent.Id
                });
            await db
                .Query("SeriesTorrentMapping")
                .InsertAsync(
                    ["SeriesTorrentId", "EpisodeId", "TorrentFileId", "Name"],
                    torrent.Mappings.Select(m => new object[]{
                        seriesTorrentId,
                        m.EpisodeId,
                        m.TorrentFileId,
                        "Original Broadcast"
                    })
                );
        }
        transaction.Commit();
    }

    public record GetSeriesTorrentMappingAsyncResultTorrentFile(string Path, string? ImdbId);

    public record GetSeriesTorrentMappingsAsyncResultTorrent(string Url, string Name, string Source, List<GetSeriesTorrentMappingAsyncResultTorrentFile> Files);

    public record GetSeriesTorrentMappingsAsyncResult(List<GetSeriesTorrentMappingsAsyncResultTorrent> Torrents);

    private record GetSeriesTorrentMappingsAsyncRow(string TorrentUrl, string TorrentName, string TorrentSource, string TorrentFilePath, string? EpisodeImdbId);

    public async Task<GetSeriesTorrentMappingsAsyncResult> GetSeriesTorrentMappingsAsync(string imdbId)
    {
        if(! await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Series")
            .Join("SeriesTorrent", j => j.On("Series.Id", "SeriesTorrent.SeriesId"))
            .Join("Torrent", j => j.On("SeriesTorrent.TorrentId", "Torrent.Id"))
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .LeftJoin("SeriesTorrentMapping", j => j.On("TorrentFile.Id", "SeriesTorrentMapping.TorrentFileId"))
            .LeftJoin("SeriesEpisode", j => j.On("SeriesTorrentMapping.EpisodeId", "SeriesEpisode.Id"))
            .Select(["Torrent.Url AS TorrentUrl", "Torrent.Name AS TorrentName", "Torrent.Source AS TorrentSource", "TorrentFile.Path AS TorrentFilePath", "SeriesEpisode.ImdbId AS EpisodeImdbId"])
            .GetAsync<GetSeriesTorrentMappingsAsyncRow>();
        GetSeriesTorrentMappingsAsyncResult result = new([]);
        foreach(var row in rows)
        {
            GetSeriesTorrentMappingsAsyncResultTorrent? torrent = result.Torrents.FirstOrDefault(t => t.Url == row.TorrentUrl);
            if(torrent == null){
                result.Torrents.Add(torrent = new(row.TorrentUrl, row.TorrentName, row.TorrentSource, []));
            }
            torrent.Files.Add(new(row.TorrentFilePath, row.EpisodeImdbId));
        }
        return result;
    }

    public async Task SetSeriesStorageLocationsAsync(string imdbId, List<string> storageLocations)
    {
        if(! await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("SeriesStorageLocation")
            .WhereIn("SeriesStorageLocation.Id", db
                .Query("Series")
                .Join("SeriesStorageLocation", j => j.On("Series.Id", "SeriesStorageLocation.SeriesId"))
                .Where("Series.ImdbId", imdbId)
                .Select("SeriesStorageLocation.Id")
            )
            .DeleteAsync();
        int seriesId = await db
            .Query("Series")
            .Where("ImdbId", imdbId)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
        await db
            .Query("SeriesStorageLocation")
            .InsertAsync(["SeriesId", "StorageLocation"], storageLocations.Select(sl => new object[]{
                seriesId,
                sl
            }));
        transaction.Commit();
    }

    public async Task<List<string>> GetSeriesStorageLocationsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("Series")
            .Join("SeriesStorageLocation", j => j.On("Series.Id", "SeriesStorageLocation.SeriesId"))
            .Where("Series.ImdbId", imdbId)
            .Select("SeriesStorageLocation.StorageLocation")
            .GetAsync<string>())
            .ToList();
    }

    public async Task<int> GetSeriesEpisodeIdByImdbIdAsync(string episodeImdbId)
    {
        using var db = Db.CreateConnection();
        return await db
            .Query("SeriesEpisode")
            .Where("ImdbId", episodeImdbId)
            .Select("Id")
            .FirstAsync<int>();
    }
}