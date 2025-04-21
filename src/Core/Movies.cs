using ReelGrab.Database;
using ReelGrab.MediaIndexes;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using ReelGrab.TorrentClients.Exceptions;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    public async Task<bool> MovieWithImdbIdExistsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("Movie").Where("ImdbId", imdbId).CountAsync<int>()) > 0;
    }

    public async Task<bool> MovieWithIdExistsAsync(int id)
    {
        using var db = Db.CreateConnection();
        return (await db.Query("Movie").Where("Id", id).CountAsync<int>()) > 0;
    }

    public async Task<int> AddMovieAsync(string imdbId, string name, string? description, string? poster, int year, bool wanted)
    {
        if (await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} already exists");
        }
        MediaType type = await MediaIndex.instance.GetMediaTypeByImdbIdAsync(imdbId);
        if (type != MediaType.MOVIE)
        {
            throw new Exception($"{imdbId} is of type {type}, not movie");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        try
        {
            await db.Query("Movie").InsertAsync(new
            {
                ImdbId = imdbId,
                Name = name,
                Description = description,
                Poster = poster,
                Year = year,
                Wanted = wanted ? 1 : 0
            });
            transaction.Commit();
            return await db
                .Query("Movie")
                .Where("ImdbId", imdbId)
                .Select("Id")
                .FirstOrDefaultAsync<int>();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task SetMovieWantedAsync(string imdbId, bool wanted)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exists");
        }
        using var db = Db.CreateConnection();
        await db
            .Query("Movie")
            .Where("ImdbId", imdbId)
            .UpdateAsync(new
            {
                Wanted = wanted ? 1 : 0
            });
    }

    private record SetMovieCinematicCutTorrentAsyncExistingCinematicCutTorrentRow(long? MovieTorrentId, long? MediaTorrentFileId, long Count);

    public async Task SetMovieCinematicCutTorrentAsync(string imdbId, int torrentId, int torrentFileId)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exists");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        var existingCinematicCutTorrent = await db
            .Query("Movie AS m")
            .LeftJoin("MovieTorrent AS mt", j => j.On("m.Id", "mt.MovieId"))
            .LeftJoin("MovieTorrentFile AS mtf1", j => j.On("mt.Id", "mtf1.MovieTorrentId").On("mtf1.Name", "Cinematic Cut"))
            .LeftJoin("MovieTorrentFile AS mtf2", j => j.On("mt.Id", "mtf2.MovieTorrentId"))
            .Where("m.ImdbId", imdbId)
            .GroupBy(["mt.Id", "mtf1.Id"])
            .SelectRaw("mt.Id AS MovieTorrentId, mtf1.Id AS MediaTorrentFileId, COALESCE(CAST(COUNT(mtf2.Id) AS INTEGER), 0) AS Count")
            .FirstOrDefaultAsync<SetMovieCinematicCutTorrentAsyncExistingCinematicCutTorrentRow>();
        if (existingCinematicCutTorrent.MovieTorrentId != null)
        {
            await db
                .Query("MovieTorrentFile")
                .Where("Id", existingCinematicCutTorrent.MediaTorrentFileId)
                .DeleteAsync();
            if (existingCinematicCutTorrent.Count == 1)
            {
                await db
                    .Query("MovieTorrent")
                    .Where("Id", existingCinematicCutTorrent.MovieTorrentId)
                    .DeleteAsync();
            }
        }
        if (existingCinematicCutTorrent.MovieTorrentId == null || (existingCinematicCutTorrent != null && existingCinematicCutTorrent.Count == 1))
        {
            int movieId = await db
                .Query("Movie")
                .Where("Imdbid", imdbId)
                .Select("Id")
                .FirstOrDefaultAsync<int>();
            int movieTorrentId = await db.Query("MovieTorrent")
                .InsertGetIdAsync<int>(new
                {
                    MovieId = movieId,
                    TorrentId = torrentId
                });
            await db
                .Query("MovieTorrentFile")
                .InsertAsync(new
                {
                    MovieTorrentId = movieTorrentId,
                    TorrentFileId = torrentFileId,
                    Name = "Cinematic Cut"
                });
        }
        else
        {
            await db
                .Query("MovieTorrentFile")
                .InsertAsync(new
                {
                    existingCinematicCutTorrent!.MovieTorrentId,
                    TorrentFileId = torrentFileId,
                    Name = "Cinematic Cut"
                });
        }
        transaction.Commit();
    }

    public record GetMovieCinematicCutTorrentAsyncTorrentFile(int Id, string Path, int Bytes, bool Mapped);

    public record GetMovieCinematicCutTorrentAsyncTorrent(int Id, string Url, string Hash, string Name, string Source, List<GetMovieCinematicCutTorrentAsyncTorrentFile> Files);

    public record GetMovieCinematicCutTorrentAsyncResult(GetMovieCinematicCutTorrentAsyncTorrent? Torrent);

    private record GetMovieCinematicCutTorrentAsyncTorrentRow(long TorrentId, string TorrentUrl, string TorrentHash, string TorrentName, string TorrentSource, long MovieTorrentId);

    private record GetMovieCinematicCutTorrentAsyncTorrentFileRow(long TorrentFileId, string TorrentFilePath, long TorrentFileBytes, string? MovieTorrentFileName);

    public async Task<GetMovieCinematicCutTorrentAsyncResult> GetMovieCinematicCutTorrentAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        var torrent = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId").On("TorrentFile.Id", "MovieTorrentFile.TorrentFileId"))
            .Where("Movie.ImdbId", imdbId)
            .Where("MovieTorrentFile.Name", "Cinematic Cut")
            .Select(["Torrent.Id AS TorrentId", "Torrent.Url AS TorrentUrl", "Torrent.Hash AS TorrentHash", "Torrent.Name AS TorrentName", "Torrent.Source AS TorrentSource", "MovieTorrent.Id AS MovieTorrentId"])
            .FirstOrDefaultAsync<GetMovieCinematicCutTorrentAsyncTorrentRow>();
        if (torrent == null)
        {
            return new(null);
        }
        var files = await db
            .Query("Torrent")
            .Join("MovieTorrent", j => j.On("Torrent.Id", "MovieTorrent.TorrentId"))
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .LeftJoin("MovieTorrentFile", j => j.On("TorrentFile.Id", "MovieTorrentFile.TorrentFileId"))
            .Where("Torrent.Id", torrent.TorrentId)
            .Where("MovieTorrent.Id", torrent.MovieTorrentId)
            .Select(["TorrentFile.Id AS TorrentFileId", "TorrentFile.Path AS TorrentFilePath", "TorrentFile.Bytes AS TorrentFileBytes", "MovieTorrentFile.Name AS MovieTorrentFileName"])
            .GetAsync<GetMovieCinematicCutTorrentAsyncTorrentFileRow>();
        return new(new(
            Id: (int)torrent.TorrentId,
            Url: torrent.TorrentUrl,
            Hash: torrent.TorrentHash,
            Name: torrent.TorrentName,
            Source: torrent.TorrentSource,
            files.Select(f => new GetMovieCinematicCutTorrentAsyncTorrentFile(
                Id: (int)f.TorrentFileId,
                Path: f.TorrentFilePath,
                Bytes: (int)f.TorrentFileBytes,
                Mapped: f.MovieTorrentFileName != null && f.MovieTorrentFileName == "Cinematic Cut"
            )).ToList()
        ));
    }

    public async Task SetMovieStorageLocationsAsync(string imdbId, List<string> storageLocations)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        await db
            .Query("MovieStorageLocation")
            .WhereIn("MovieStorageLocation.Id", db
                .Query("Movie")
                .Join("MovieStorageLocation", j => j.On("Movie.Id", "MovieStorageLocation.MovieId"))
                .Where("Movie.ImdbId", imdbId)
                .Select("MovieStorageLocation.Id")
            )
            .DeleteAsync();
        int movieId = await db
            .Query("Movie")
            .Where("ImdbId", imdbId)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
        await db
            .Query("MovieStorageLocation")
            .InsertAsync(["MovieId", "StorageLocation"], storageLocations.Select(sl => new object[]{
                movieId,
                sl
            }));
        transaction.Commit();
    }

    public async Task<List<string>> GetMovieStorageLocationsAsync(int movieId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("Movie")
            .Join("MovieStorageLocation", j => j.On("Movie.Id", "MovieStorageLocation.MovieId"))
            .Where("Movie.Id", movieId)
            .Select("MovieStorageLocation.StorageLocation")
            .GetAsync<string>())
            .ToList();
    }

    public async Task<List<string>> GetMovieStorageLocationsAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("Movie")
            .Join("MovieStorageLocation", j => j.On("Movie.Id", "MovieStorageLocation.MovieId"))
            .Where("Movie.ImdbId", imdbId)
            .Select("MovieStorageLocation.StorageLocation")
            .GetAsync<string>())
            .ToList();
    }

    public record MovieDetails(string ImdbId, string Name);

    public async Task<MovieDetails> GetMovieDetailsAsync(int movieId)
    {
        using var db = Db.CreateConnection();
        return await db
            .Query("Movie")
            .Where("Id", movieId)
            .Select(["ImdbId", "Name"])
            .FirstAsync<MovieDetails>();
    }

    public async Task<bool> MovieIsSavedSomewhereAsync(int movieId)
    {
        foreach (var storageLocation in await GetMovieStorageLocationsAsync(movieId))
        {
            IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocation);
            if (storage != null && await storage.HasMovieSavedAsync(movieId, "Cinematic Cut"))
            {
                return true;
            }
        }
        return false;
    }

    public record GetMoviesInProgressMovie(int Id, string ImdbId, string Name, List<string> StorageLocations, int Progress);

    private record GetMoviesInProgressMovieRow(long Id, string ImdbId, string Name, string Hash, string Path);

    public async Task<List<GetMoviesInProgressMovie>> GetMoviesInProgressAsync()
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Where("MovieTorrentFile.Name", "Cinematic Cut")
            .Select(["Movie.Id", "Movie.ImdbId", "Movie.Name", "Torrent.Hash", "TorrentFile.Path"])
            .GetAsync<GetMoviesInProgressMovieRow>();
        List<GetMoviesInProgressMovie> movies = [];
        foreach(var row in rows)
        {
            int progress;
            try{
                List<ITorrentClient.TorrentFileInfo> torrentFiles = await TorrentClient.instance.GetTorrentFilesByHashAsync(row.Hash);
                progress = torrentFiles.First(tf => tf.Path == row.Path).Progress;
            } catch(TorrentDoesNotExistException){
                progress = await MovieIsSavedSomewhereAsync((int)row.Id)
                    ? 100
                    : 0;
            }
                movies.Add(new(
                    (int)row.Id,
                    row.ImdbId,
                    row.Name,
                    await GetMovieStorageLocationsAsync((int)row.Id),
                    progress
                ));
        }
        return movies;
    }
}