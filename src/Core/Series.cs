using ReelGrab.Database;
using ReelGrab.MediaIndexes;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using ReelGrab.TorrentClients.Exceptions;
using ReelGrab.Utils;
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

    public record UpdateSeriesEpisodesAsyncEpisode(int Number, string ImdbId, string Name, bool Wanted);

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
                                Wanted = episode.Wanted ? 1 : 0
                            });
                        continue;
                    }
                    bool episodeUpdatable = (await db
                        .Query("SeriesEpisode")
                        .Where("SeasonId", seasonId)
                        .Where("ImdbId", episode.ImdbId)
                        .Where("Number", episode.Number)
                        .Where("Name", episode.Name)
                        .Where("Wanted", episode.Wanted ? 1 : 0)
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
                                ImdbId = episode.ImdbId,
                                Wanted = episode.Wanted ? 1 : 0
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

    private record SetSeriesTorrentMappingsAsyncOldMapping(long Id, long TorrentId, long TorrentFileId, long EpisodeId, long SeriesTorrentId);

    public async Task SetSeriesTorrentMappingsAsync(string imdbId, List<SetSeriesTorrentMappingsAsyncTorrent> newTorrents)
    {
        if (!await SeriesWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        int seriesId = await db
            .Query("Series")
            .Where("ImdbId", imdbId)
            .Select(["Id"])
            .FirstAsync<int>();
        var storageLocationRecords = await GetSeriesStorageLocationRecordsAsync(seriesId);
        var oldMappings = await db
            .Query("SeriesTorrentMapping")
            .Join("SeriesTorrent", j => j.On("SeriesTorrentMapping.SeriesTorrentId", "SeriesTorrent.Id"))
            .Where("SeriesTorrent.SeriesId", seriesId)
            .Where("SeriesTorrentMapping.Name", "Original Broadcast")
            .Select(["SeriesTorrentMapping.Id", "SeriesTorrent.TorrentId", "SeriesTorrentMapping.TorrentFileId", "SeriesTorrentMapping.EpisodeId", "SeriesTorrent.Id as SeriesTorrentId"])
            .GetAsync<SetSeriesTorrentMappingsAsyncOldMapping>();
        foreach (var newTorrent in newTorrents)
        {
            int seriesTorrentId = await db
                .Query("SeriesTorrent")
                .Where("SeriesId", seriesId)
                .Where("TorrentId", newTorrent.Id)
                .Select("Id")
                .FirstOrDefaultAsync<int?>()
                ?? await db
                    .Query("SeriesTorrent")
                    .InsertGetIdAsync<int>(new
                    {
                        SeriesId = seriesId,
                        TorrentId = newTorrent.Id
                    });
            foreach (var newMapping in newTorrent.Mappings)
            {
                var oldMapping = oldMappings.FirstOrDefault(m => m.EpisodeId == newMapping.EpisodeId);
                if (oldMapping != null)
                {
                    if (oldMapping.TorrentId == newTorrent.Id && oldMapping.TorrentFileId == newMapping.TorrentFileId)
                    {
                        continue;
                    }
                    await db
                        .Query("SeriesOutputFile")
                        .Where("SeriesId", seriesId)
                        .Where("EpisodeId", newMapping.EpisodeId)
                        .Where("Name", "Original Broadcast")
                        .WhereIn("Status", ["InitializedPendingCreation", "StalePendingUpdate", "Okay"])
                        .UpdateAsync(new Dictionary<string, object> {
                            #pragma warning disable CS8625
                            { "SeriesTorrentMappingId", null }
                            #pragma warning restore CS8625
                        });
                    await db
                        .Query("SeriesTorrentMapping")
                        .Where("Id", oldMapping.Id)
                        .DeleteAsync();
                }
                var id = await db
                    .Query("SeriesTorrentMapping")
                    .InsertGetIdAsync<int>(new
                    {
                        SeriesTorrentId = seriesTorrentId,
                        EpisodeId = newMapping.EpisodeId,
                        TorrentFileId = newMapping.TorrentFileId,
                        Name = "Original Broadcast"
                    });
                string path = await CreateSeriesEpisodeFilePathByImdbIdAndEpisodeIdAndTorrentFileIdAsync(imdbId, newMapping.EpisodeId, newMapping.TorrentFileId);
                foreach (var storageLocationRecord in storageLocationRecords)
                {
                    int? oldRecordId = await db
                        .Query("SeriesOutputFile")
                        .Where("SeriesId", seriesId)
                        .Where("EpisodeId", newMapping.EpisodeId)
                        .Where("SeriesStorageLocationId", storageLocationRecord.Id)
                        .Where("Name", "Original Broadcast")
                        .Where("FilePath", path)
                        .Select("Id")
                        .FirstOrDefaultAsync<int?>();
                    if (oldRecordId != null)
                    {
                        await db
                            .Query("SeriesOutputFile")
                            .Where("Id", oldRecordId)
                            .UpdateAsync(new
                            {
                                SeriesTorrentMappingId = id,
                                Status = "StalePendingUpdate"
                            });
                    }
                    else
                    {
                        await db
                            .Query("SeriesOutputFile")
                            .Where("SeriesId", seriesId)
                            .Where("EpisodeId", newMapping.EpisodeId)
                            .Where("SeriesStorageLocationId", storageLocationRecord.Id)
                            .Where("Name", "Original Broadcast")
                            .UpdateAsync(new
                            {
                                Status = "MisplacedPendingDeletion"
                            });
                        await db
                            .Query("SeriesOutputFile")
                            .InsertAsync(new
                            {
                                SeriesId = seriesId,
                                EpisodeId = newMapping.EpisodeId,
                                SeriesTorrentMappingId = id,
                                SeriesStorageLocationId = storageLocationRecord.Id,
                                StorageLocation = storageLocationRecord.StorageLocation,
                                Name = "Original Broadcast",
                                FilePath = path,
                                Status = "InitializedPendingCreation"
                            });
                    }
                }
            }
        }
        foreach (var oldMapping in oldMappings)
        {
            if (newTorrents.Any(t => t.Id == oldMapping.TorrentId && t.Mappings.Any(m => m.TorrentFileId == oldMapping.TorrentFileId && m.EpisodeId == oldMapping.EpisodeId)))
            {
                continue;
            }
            await db
                .Query("SeriesOutputFile")
                .Where("SeriesId", seriesId)
                .Where("EpisodeId", oldMapping.EpisodeId)
                .UpdateAsync(new Dictionary<string, object> {
                    #pragma warning disable CS8625
                    { "SeriesTorrentMappingId", null },
                    { "Status", "MisplacedPendingDeletion" }
                    #pragma warning restore CS8625
                });
            await db
                .Query("SeriesTorrentMapping")
                .Where("Id", oldMapping.Id)
                .DeleteAsync();
            int remainingSeriesTorrentMappingCount = await db
                .Query("SeriesTorrentMapping")
                .Where("SeriesTorrentId", oldMapping.SeriesTorrentId)
                .CountAsync<int>();
            if (remainingSeriesTorrentMappingCount == 0)
            {
                await db
                    .Query("SeriesTorrent")
                    .Where("Id", oldMapping.SeriesTorrentId)
                    .DeleteAsync();
            }
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
            .Where("Series.ImdbId", imdbId)
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

    private record SetSeriesStorageLocationsAsyncMappingRow(long Id, long EpisodeId, long TorrentFileId, string Name);

    public async Task SetSeriesStorageLocationsAsync(string imdbId, List<string> storageLocations)
    {
        if (!await SeriesWithImdbIdExistsAsync(imdbId))
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
        var mappings = await db
            .Query("SeriesTorrentMapping")
            .Join("SeriesTorrent", j => j.On("SeriesTorrentMapping.SeriesTorrentId", "SeriesTorrent.Id"))
            .Where("SeriesTorrent.SeriesId", seriesId)
            .Select(["SeriesTorrentMapping.Id", "EpisodeId", "SeriesTorrentMapping.TorrentFileId", "SeriesTorrentMapping.Name"])
            .GetAsync<SetSeriesStorageLocationsAsyncMappingRow>();
        Dictionary<long, string> paths = new();
        foreach (var mapping in mappings)
        {
            paths[mapping.Id] = await CreateSeriesEpisodeFilePathByImdbIdAndEpisodeIdAndTorrentFileIdAsync(imdbId, (int)mapping.EpisodeId, (int)mapping.TorrentFileId);
        }
        var storageLocationRecords = await GetSeriesStorageLocationRecordsAsync(seriesId);
        foreach (var storageLocation in storageLocations)
        {
            if (storageLocationRecords.Any(slr => slr.StorageLocation == storageLocation))
            {
                continue;
            }
            int id = await db
                .Query("SeriesStorageLocation")
                .InsertGetIdAsync<int>(new
                {
                    SeriesId = seriesId,
                    StorageLocation = storageLocation
                });
            await db
                .Query("SeriesOutputFile")
                .InsertAsync(["SeriesId", "EpisodeId", "SeriesTorrentMappingId", "SeriesStorageLocationId", "StorageLocation", "Name", "FilePath", "Status"],
                    mappings.Select(m => new object[] {
                        seriesId,
                        m.EpisodeId,
                        m.Id,
                        id,
                        storageLocation,
                        m.Name,
                        paths[m.Id],
                        "InitializedPendingCreation"
                    }));
        }
        foreach (var storageLocationRecord in storageLocationRecords)
        {
            if (storageLocations.Any(sl => sl == storageLocationRecord.StorageLocation))
            {
                continue;
            }
            await db
                .Query("SeriesOuputFile")
                .Where("SeriesStorageLocationId", storageLocationRecord.Id)
                .UpdateAsync(new Dictionary<string, object> {
                    #pragma warning disable CS8625
                    { "SeriesStorageLocationId", null },
                    { "Status", "MisplacedPendingDeletion" }
                    #pragma warning restore CS8625
                });
            await db
                .Query("SeriesStorageLocation")
                .Where("SeriesStorageLocationId", storageLocationRecord.Id)
                .DeleteAsync();
        }
        transaction.Commit();
    }

    public async Task<List<string>> GetSeriesStorageLocationsAsync(int id)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("Series")
            .Join("SeriesStorageLocation", j => j.On("Series.Id", "SeriesStorageLocation.SeriesId"))
            .Where("Series.Id", id)
            .Select("SeriesStorageLocation.StorageLocation")
            .GetAsync<string>())
            .ToList();
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

    public record SeriesStorageLocationRecord(int Id, string StorageLocation);

    private record GetSeriesStorageLocationRecordsAsyncRow(long Id, string StorageLocation);

    public async Task<List<SeriesStorageLocationRecord>> GetSeriesStorageLocationRecordsAsync(int seriesId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("SeriesStorageLocation")
            .Where("SeriesId", seriesId)
            .Select(["Id", "StorageLocation"])
            .GetAsync<GetSeriesStorageLocationRecordsAsyncRow>())
            .Select(r => new SeriesStorageLocationRecord(
                Id: (int)r.Id,
                StorageLocation: r.StorageLocation
            ))
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

    public record SeriesEpisodeDetails(string SeriesName, string SeriesImdbId, int SeasonId, int SeasonNumber, int EpisodeNumber, string EpisodeName, string EpisodeImdbId);

    private record GetSeriesEpisodeDetailsAsyncRow(string SeriesName, string SeriesImdbId, long SeasonId, long SeasonNumber, long EpisodeNumber, string EpisodeName, string EpisodeImdbId);

    public async Task<SeriesEpisodeDetails> GetSeriesEpisodeDetailsAsync(int episodeId)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("SeriesEpisode")
            .Join("SeriesSeason", j => j.On("SeriesEpisode.SeasonId", "SeriesSeason.Id"))
            .Join("Series", j => j.On("SeriesSeason.SeriesId", "Series.Id"))
            .Select(["Series.Name AS SeriesName", "Series.ImdbId AS SeriesImdbId", "SeriesSeason.Id AS SeasonId", "SeriesSeason.Number AS SeasonNumber", "SeriesEpisode.Number AS EpisodeNumber", "SeriesEpisode.Name AS EpisodeName", "SeriesEpisode.ImdbId AS EpisodeImdbId"])
            .Where("SeriesEpisode.Id", episodeId)
            .FirstAsync<GetSeriesEpisodeDetailsAsyncRow>();
        return new(row.SeriesName, row.SeriesImdbId, (int)row.SeasonId, (int)row.SeasonNumber, (int)row.EpisodeNumber, row.EpisodeName, row.EpisodeImdbId);
    }

    public async Task<bool> SeriesEpisodeIsSavedSomewhereAsync(int episodeId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("SeriesOutputFile")
            .Join("SeriesTorrentMapping", j => j.On("SeriesOutputFile.SeriesTorrentMappingId", "SeriesTorrentMapping.Id"))
            .Where("SeriesTorrentMapping.EpisodeId", episodeId)
            .Where("Status", "Okay")
            .CountAsync<int>()) > 0;
    }

    public record GetSeriesInProgressSeries(int Id, string ImdbId, string Name, List<GetSeriesInProgressSeriesEpisode> Episodes, List<string> StorageLocations);

    public record GetSeriesInProgressSeriesEpisode(int Season, int Episode, int Id, string ImdbId, string Name, int Progress);

    private record GetSeriesInProgressSeriesRow(long SeriesId, string SeriesImdbId, string Name, string Hash, string Path, long EpisodeNumber, long SeasonNumber, long EpisodeId, string EpisodeImdbId, string EpisodeName);

    public async Task<List<GetSeriesInProgressSeries>> GetSeriesInProgressAsync()
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Series")
            .Join("SeriesTorrent", j => j.On("Series.Id", "SeriesTorrent.SeriesId"))
            .Join("Torrent", j => j.On("SeriesTorrent.TorrentId", "Torrent.Id"))
            .Join("SeriesTorrentMapping", j => j.On("SeriesTorrent.Id", "SeriesTorrentMapping.SeriesTorrentId"))
            .Join("TorrentFile", j => j.On("SeriesTorrentMapping.TorrentFileId", "TorrentFile.Id"))
            .Join("SeriesEpisode", j => j.On("SeriesTorrentMapping.EpisodeId", "SeriesEpisode.Id"))
            .Join("SeriesSeason", j => j.On("SeriesEpisode.SeasonId", "SeriesSeason.Id"))
            .Where("SeriesTorrentMapping.Name", "Original Broadcast")
            .Select(["Series.Id AS SeriesId", "Series.ImdbId AS SeriesImdbId", "Series.Name", "Torrent.Hash", "TorrentFile.Path", "SeriesEpisode.Number AS EpisodeNumber", "SeriesSeason.Number AS SeasonNumber", "SeriesEpisode.Id AS EpisodeId", "SeriesEpisode.ImdbId AS EpisodeImdbId", "SeriesEpisode.Name AS EpisodeName"])
            .GetAsync<GetSeriesInProgressSeriesRow>();
        List<GetSeriesInProgressSeries> serieses = [];
        Dictionary<int, List<string>> storageLocationsMaps = new();
        foreach(var row in rows)
        {
            if(!storageLocationsMaps.TryGetValue((int)row.SeriesId, out List<string>? storageLocations))
            {
                storageLocationsMaps[(int)row.SeriesId] = storageLocations = await GetSeriesStorageLocationsAsync((int)row.SeriesId);
            }
            GetSeriesInProgressSeries? series = serieses.FirstOrDefault(s => s.Id == (int)row.SeriesId);
            if(series == null)
            {
                serieses.Add(series = new((int)row.SeriesId, row.SeriesImdbId, row.Name, [], storageLocations));
            }
            int progress;
            try{
                List<ITorrentClient.TorrentFileInfo> torrentFiles = await TorrentClient.instance.GetTorrentFilesByHashAsync(row.Hash);
                progress = torrentFiles.First(tf => tf.Path == row.Path).Progress;
            } catch(TorrentDoesNotExistException){
                progress = await SeriesEpisodeIsSavedSomewhereAsync((int)row.EpisodeId)
                    ? 100
                    : 0;
            }
            series.Episodes.Add(new((int)row.SeasonNumber, (int)row.EpisodeNumber, (int)row.EpisodeId, row.EpisodeImdbId, row.EpisodeName, progress));
        }
        return serieses;
    }

    private record CreateSeriesEpisodeFilePathByImdbIdAndEpisodeIdAndTorrentFileIdAsyncSeries(string Name, long StartYear, long Season, long Episode);

    public async Task<string> CreateSeriesEpisodeFilePathByImdbIdAndEpisodeIdAndTorrentFileIdAsync(string imdbId, int episodeId, int torrentFileId)
    {
        using var db = Db.CreateConnection();
        var series = await db
            .Query("SeriesEpisode")
            .Join("SeriesSeason", j => j.On("SeriesEpisode.SeasonId", "SeriesSeason.Id"))
            .Join("Series", j => j.On("SeriesSeason.SeriesId", "Series.Id"))
            .Where("Series.ImdbId", imdbId)
            .Where("SeriesEpisode.Id", episodeId)
            .Select(["Series.Name", "StartYear", "SeriesSeason.Number AS Season", "SeriesEpisode.Number AS Episode"])
            .FirstAsync<CreateSeriesEpisodeFilePathByImdbIdAndEpisodeIdAndTorrentFileIdAsyncSeries>();
        var path = await db
            .Query("TorrentFile")
            .Where("Id", torrentFileId)
            .Select("Path")
            .FirstAsync<string>();
        return CreateSeriesEpisodeFilePath(series.Name, (int)series.StartYear, (int)series.Season, (int)series.Episode, Path.GetExtension(path).Replace(".", ""));
    }

    public string CreateSeriesEpisodeFilePath(string seriesName, int seriesStartYear, int season, int episode, string extension)
    {
        return $"{seriesName} ({seriesStartYear})/S{SeriesFormatting.FormatSeason(season)}/{seriesName} S{SeriesFormatting.FormatSeason(season)}E{SeriesFormatting.FormatEpisode(episode)}.{extension}";
    }
}