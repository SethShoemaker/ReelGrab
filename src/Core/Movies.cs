using ReelGrab.Database;
using ReelGrab.MediaIndexes;
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

    private record SetMovieTheatricalCutTorrentAsyncExistingTheatricalReleaseTorrentRow(long? MovieTorrentId, long? MediaTorrentFileId, long Count);

    public async Task SetMovieTheatricalReleaseTorrentAsync(string imdbId, int torrentId, int torrentFileId)
    {
        if (!await MovieWithImdbIdExistsAsync(imdbId))
        {
            throw new Exception($"{imdbId} does not exists");
        }
        using var db = Db.CreateConnection();
        using var transaction = db.Connection.BeginTransaction();
        var existingTheatricalReleaseTorrent = await db
            .Query("Movie AS m")
            .LeftJoin("MovieTorrent AS mt", j => j.On("m.Id", "mt.MovieId"))
            .LeftJoin("MovieTorrentFile AS mtf1", j => j.On("mt.Id", "mtf1.MovieTorrentId").On("mtf1.Name", "Theatrical Release"))
            .LeftJoin("MovieTorrentFile AS mtf2", j => j.On("mt.Id", "mtf2.MovieTorrentId"))
            .Where("m.ImdbId", imdbId)
            .GroupBy(["mt.Id", "mtf1.Id"])
            .SelectRaw("mt.Id AS MovieTorrentId, mtf1.Id AS MediaTorrentFileId, COALESCE(CAST(COUNT(mtf2.Id) AS INTEGER), 0) AS Count")
            .FirstOrDefaultAsync<SetMovieTheatricalCutTorrentAsyncExistingTheatricalReleaseTorrentRow>();
        if (existingTheatricalReleaseTorrent.MovieTorrentId != null)
        {
            await db
                .Query("MovieTorrentFile")
                .Where("Id", existingTheatricalReleaseTorrent.MediaTorrentFileId)
                .DeleteAsync();
            if (existingTheatricalReleaseTorrent.Count == 1)
            {
                await db
                    .Query("MovieTorrent")
                    .Where("Id", existingTheatricalReleaseTorrent.MovieTorrentId)
                    .DeleteAsync();
            }
        }
        if (existingTheatricalReleaseTorrent.MovieTorrentId == null || (existingTheatricalReleaseTorrent != null && existingTheatricalReleaseTorrent.Count == 1))
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
                    Name = "Theatrical Release"
                });
        }
        else
        {
            await db
                .Query("MovieTorrentFile")
                .InsertAsync(new
                {
                    existingTheatricalReleaseTorrent!.MovieTorrentId,
                    TorrentFileId = torrentFileId,
                    Name = "Theatrical Release"
                });
        }
        transaction.Commit();
    }

    public record GetMovieTheatricalReleaseTorrentAsyncTorrentFile(int Id, string Path, int Bytes, bool Mapped);

    public record GetMovieTheatricalReleaseTorrentAsyncTorrent(int Id, string Url, string Hash, string Name, string Source, List<GetMovieTheatricalReleaseTorrentAsyncTorrentFile> Files);

    public record GetMovieTheatricalReleaseTorrentAsyncResult(GetMovieTheatricalReleaseTorrentAsyncTorrent? Torrent);

    private record GetMovieTheatricalReleaseTorrentAsyncTorrentRow(long TorrentId, string TorrentUrl, string TorrentHash, string TorrentName, string TorrentSource, long MovieTorrentId);

    private record GetMovieTheatricalReleaseTorrentAsyncTorrentFileRow(long TorrentFileId, string TorrentFilePath, long TorrentFileBytes, string? MovieTorrentFileName);

    public async Task<GetMovieTheatricalReleaseTorrentAsyncResult> GetMovieTheatricalReleaseTorrentAsync(string imdbId)
    {
        using var db = Db.CreateConnection();
        var torrent = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId").On("TorrentFile.Id", "MovieTorrentFile.TorrentFileId"))
            .Where("Movie.ImdbId", imdbId)
            .Where("MovieTorrentFile.Name", "Theatrical Release")
            .Select(["Torrent.Id AS TorrentId", "Torrent.Url AS TorrentUrl", "Torrent.Hash AS TorrentHash", "Torrent.Name AS TorrentName", "Torrent.Source AS TorrentSource", "MovieTorrent.Id AS MovieTorrentId"])
            .FirstOrDefaultAsync<GetMovieTheatricalReleaseTorrentAsyncTorrentRow>();
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
            .GetAsync<GetMovieTheatricalReleaseTorrentAsyncTorrentFileRow>();
        return new(new(
            Id: (int)torrent.TorrentId,
            Url: torrent.TorrentUrl,
            Hash: torrent.TorrentHash,
            Name: torrent.TorrentName,
            Source: torrent.TorrentSource,
            files.Select(f => new GetMovieTheatricalReleaseTorrentAsyncTorrentFile(
                Id: (int)f.TorrentFileId,
                Path: f.TorrentFilePath,
                Bytes: (int)f.TorrentFileBytes,
                Mapped: f.MovieTorrentFileName != null && f.MovieTorrentFileName == "Theatrical Release"
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
}