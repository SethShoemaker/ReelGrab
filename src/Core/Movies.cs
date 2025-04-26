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

    private record SetMovieCinematicCutTorrentAsyncExistingCinematicCutTorrentRow(long MovieTorrentId, long TorrentId, long MovieTorrentFileId, long TorrentFileId);

    public async Task SetMovieCinematicCutTorrentAsync(string imdbId, int torrentId, int torrentFileId)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exists");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        var existingCinematicCutTorrent = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId"))
            .Where("Movie.ImdbId", imdbId)
            .Where("MovieTorrentFile.Name", "Cinematic Cut")
            .Select(["MovieTorrent.Id AS MovieTorrentId", "MovieTorrent.TorrentId", "MovieTorrentFile.Id AS MovieTorrentFileId", "MovieTorrentFile.TorrentFileId"])
            .FirstOrDefaultAsync<SetMovieCinematicCutTorrentAsyncExistingCinematicCutTorrentRow>();
        if (existingCinematicCutTorrent != null && existingCinematicCutTorrent.TorrentId == torrentId && existingCinematicCutTorrent.TorrentFileId == torrentFileId)
        {
            return;
        }
        int movieId = await db
            .Query("Movie")
            .Where("ImdbId", imdbId)
            .Select("Id")
            .FirstAsync<int>();
        int movieTorrentId = await db
            .Query("MovieTorrent")
            .Where("MovieId", movieId)
            .Where("TorrentId", torrentId)
            .Select("Id")
            .FirstOrDefaultAsync<int?>()
            ?? await db
                .Query("MovieTorrent")
                .InsertGetIdAsync<int>(new
                {
                    MovieId = movieId,
                    TorrentId = torrentId
                });
        int movieTorrentFileId = await db
            .Query("MovieTorrentFile")
            .InsertGetIdAsync<int>(new
            {
                MovieTorrentId = movieTorrentId,
                TorrentFileId = torrentFileId,
                Name = "Cinematic Cut"
            });
        string outputFilePath = await CreateMovieFilePathByImdbIdAndTorrentFileIdAsync(imdbId, "Cinematic Cut", torrentFileId);
        foreach (var storageLocationRecord in await GetMovieStorageLocationRecordsAsync(movieId))
        {
            int? staleOutputFileRecordId = await db
                .Query("MovieOutputFile")
                .Where("MovieId", movieId)
                .Where("MovieStorageLocationId", storageLocationRecord.Id)
                .Where("Name", "Cinematic Cut")
                .Select("Id")
                .FirstOrDefaultAsync<int?>();
            if (staleOutputFileRecordId != null)
            {
                await db
                    .Query("MovieOutputFile")
                    .Where("Id", staleOutputFileRecordId)
                    .UpdateAsync(new
                    {
                        MovieTorrentFileId = movieTorrentFileId,
                        Status = "StalePendingUpdate"
                    });
            }
            else
            {
                await db
                    .Query("MovieOutputFile")
                    .InsertAsync(new
                    {
                        MovieId = movieId,
                        MovieTorrentFileId = movieTorrentFileId,
                        MovieStorageLocationId = storageLocationRecord.Id,
                        StorageLocation = storageLocationRecord.StorageLocation,
                        Name = "Cinematic Cut",
                        FilePath = outputFilePath,
                        Status = "InitializedPendingCreation"
                    });
            }
        }
        if (existingCinematicCutTorrent != null)
        {
            await db
                .Query("MovieTorrentFile")
                .Where("Id", existingCinematicCutTorrent.MovieTorrentFileId)
                .DeleteAsync();
            int remainingMovieTorrentFileCount = await db
                .Query("MovieTorrentFile")
                .Where("MovieTorrentId", existingCinematicCutTorrent.MovieTorrentId)
                .CountAsync<int>();
            if (remainingMovieTorrentFileCount == 0)
            {
                await db
                    .Query("MovieTorrent")
                    .Where("Id", existingCinematicCutTorrent.MovieTorrentId)
                    .DeleteAsync();
            }
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

    private record SetMovieStorageLocationsAsyncMovieTorrentFileRow(long Id, string Name, string Path);

    public async Task SetMovieStorageLocationsAsync(string imdbId, List<string> storageLocations)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exist");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        int movieId = await db
            .Query("Movie")
            .Where("ImdbId", imdbId)
            .Select("Id")
            .FirstOrDefaultAsync<int>();
        var movieTorrentFiles = await db
            .Query("MovieTorrentFile")
            .Join("MovieTorrent", j => j.On("MovieTorrentFile.MovieTorrentId", "MovieTorrent.Id"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Where("MovieTorrent.MovieId", movieId)
            .Select(["MovieTorrentFile.Id", "MovieTorrentFile.Name", "TorrentFile.Path"])
            .GetAsync<SetMovieStorageLocationsAsyncMovieTorrentFileRow>();
        var storageLocationRecords = await GetMovieStorageLocationRecordsAsync(movieId);
        foreach (var newStorageLocation in storageLocations)
        {
            if (storageLocationRecords.Any(slr => slr.StorageLocation == newStorageLocation))
            {
                continue;
            }
            int newStorageLocationRecordId = await db
                .Query("MovieStorageLocation")
                .InsertGetIdAsync<int>(new
                {
                    MovieId = movieId,
                    StorageLocation = newStorageLocation
                });
            foreach (var movieTorrentFile in movieTorrentFiles)
            {
                int? existingMovieOutputFileId = await db
                    .Query("MovieOutputFile")
                    .Where("MovieId", movieId)
                    .Where("StorageLocation", newStorageLocation)
                    .Where("MovieTorrentFileId", movieTorrentFile.Id)
                    .Select("Id")
                    .FirstOrDefaultAsync<int?>();
                if (existingMovieOutputFileId != null)
                {
                    await db
                        .Query("MovieOutputFile")
                        .Where("Id", existingMovieOutputFileId)
                        .UpdateAsync(new
                        {
                            MovieStorageLocationId = newStorageLocationRecordId,
                            Status = "InitializedPendingCreation"
                        });
                }
                else
                {
                    await db
                        .Query("MovieOutputFile")
                        .InsertAsync(new
                        {
                            MovieId = movieId,
                            MovieTorrentFileId = movieTorrentFile.Id,
                            MovieStorageLocationId = newStorageLocationRecordId,
                            StorageLocation = newStorageLocation,
                            Name = movieTorrentFile.Name,
                            FilePath = await CreateMovieFilePathByImdbIdAsync(imdbId, movieTorrentFile.Name, Path.GetExtension(movieTorrentFile.Path).Replace(".", "")),
                            Status = "InitializedPendingCreation"
                        });
                }
            }
        }
        foreach (var storageLocationRecord in storageLocationRecords)
        {
            if (storageLocations.Any(sl => sl == storageLocationRecord.StorageLocation))
            {
                continue;
            }
            await db
                .Query("MovieOutputFile")
                .Where("MovieId", movieId)
                .Where("MovieStorageLocationId", storageLocationRecord.Id)
                .UpdateAsync(new Dictionary<string, object> {
                    { "MovieStorageLocationId", null },
                    { "Status", "MisplacedPendingDeletion" }
                });
            await db
                .Query("MovieStorageLocation")
                .Where("Id", storageLocationRecord.Id)
                .DeleteAsync();
        }
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

    public record MovieStorageLocationRecord(int Id, string StorageLocation);

    private record GetMovieStorageLocationRecordsAsyncRow(long Id, string StorageLocation);

    public async Task<List<MovieStorageLocationRecord>> GetMovieStorageLocationRecordsAsync(int movieId)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("MovieStorageLocation")
            .Where("MovieId", movieId)
            .Select(["Id", "StorageLocation"])
            .GetAsync<GetMovieStorageLocationRecordsAsyncRow>())
            .Select(r => new MovieStorageLocationRecord(
                Id: (int)r.Id,
                StorageLocation: r.StorageLocation
            ))
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
        using var db = Db.CreateConnection();
        return (await db
            .Query("MovieOutputFile")
            .Join("MovieTorrentFile", j => j.On("MovieOutputFile.MovieTorrentFileId", "MovieTorrentFile.Id"))
            .Join("MovieStorageLocation", j => j.On("MovieOutputFile.MovieStorageLocationId", "MovieStorageLocation.Id"))
            .Where("MovieOutputFile.MovieId", movieId)
            .Where("MovieTorrentFile.Name", "Cinematic Cut")
            .Where("MovieOutputFile.Status", "Created")
            .CountAsync<int>()) > 0;
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

    public async Task<string> CreateMovieFilePathByImdbIdAndTorrentFileIdAsync(string imdbId, string type, int torrentFileId)
    {
        using var db = Db.CreateConnection();
        string torrentFilePath = await db
            .Query("TorrentFile")
            .Where("Id", torrentFileId)
            .Select("Path")
            .FirstAsync<string>();
        return await CreateMovieFilePathByImdbIdAsync(imdbId, "Cinematic Cut", Path.GetExtension(torrentFilePath).Replace(".", ""));
    }

    private record CreateMovieFilePathByImdbIdAsyncRow(string Name, long Year);

    public async Task<string> CreateMovieFilePathByImdbIdAsync(string imdbId, string type, string extension)
    {
        using var db = Db.CreateConnection();
        var row = await db
            .Query("Movie")
            .Where("ImdbId", imdbId)
            .Select(["Name", "Year"])
            .FirstOrDefaultAsync<CreateMovieFilePathByImdbIdAsyncRow>();
        if(row == null)
        {
            throw new Exception($"{imdbId} does not exists");
        }
        return CreateMovieFilePath(row.Name, (int)row.Year, imdbId, type, extension);
    }

    public string CreateMovieFilePath(string movieName, int movieYear, string movieImdbId, string type, string extension)
    {
        return $"{movieName} ({movieYear}) [imdbid-{movieImdbId}]/{movieName} ({movieYear}) [imdbid-{movieImdbId}] - {type}.{extension}";
    }
}