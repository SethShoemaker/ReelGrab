using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class ProcessCompletedMovies : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(60);

    public record MovieTorrent(int Id, int MovieId, string MovieImdbId, string MovieName, int MovieYear, string TorrentName, string Hash, string Path);

    public record Row(long Id, long MovieId, string MovieImdbId, string MovieName, long MovieYear, string TorrentName, string Hash, string Path);

    public async Task<List<MovieTorrent>> GetMovieTorrentsAsync(CancellationToken stoppingToken)
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Join("MovieStorageLocation", j => j.On("Movie.Id", "MovieStorageLocation.MovieId"))
            .Where("MovieTorrentFile.Name", "Cinematic Cut")
            .Select(["MovieTorrentFile.Id", "Movie.Id AS MovieId", "Movie.ImdbId AS MovieImdbId", "Movie.Name AS MovieName", "Movie.Year AS MovieYear", "Torrent.Name AS TorrentName", "Torrent.Hash", "TorrentFile.Path"])
            .GetAsync<Row>(cancellationToken: stoppingToken);
        return rows
            .Select(r => new MovieTorrent(
                Id: (int)r.Id,
                MovieId: (int)r.MovieId,
                MovieImdbId: r.MovieImdbId,
                MovieName: r.MovieName,
                MovieYear: (int)r.MovieYear,
                TorrentName: r.TorrentName,
                Hash: r.Hash,
                Path: r.Path
            ))
            .ToList();
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        // TorrentClient torrentClient = TorrentClient.instance;
        // if (!torrentClient.Implemented || !await torrentClient.ConnectionGoodAsync())
        // {
        //     return;
        // }
        // List<MovieTorrent> movieTorrents = await GetMovieTorrentsAsync(stoppingToken);
        // foreach (var movieTorrent in movieTorrents)
        // {
        //     if (await Application.instance.MovieIsSavedSomewhereAsync(movieTorrent.MovieId))
        //     {
        //         continue;
        //     }
        //     if (!await torrentClient.HasTorrentByHashAsync(movieTorrent.Hash))
        //     {
        //         continue;
        //     }
        //     ITorrentClient.TorrentFileInfo? torrentFile = (await torrentClient.GetTorrentFilesByHashAsync(movieTorrent.Hash)).FirstOrDefault(tf => tf.Path == movieTorrent.Path);
        //     if (torrentFile == null)
        //     {
        //         continue;
        //     }
        //     if (torrentFile.Progress < 100)
        //     {
        //         continue;
        //     }
        //     Stream contents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFilePathAsync(movieTorrent.Hash, movieTorrent.Path);
        //     string path = Application.instance.CreateMovieFilePath(movieTorrent.MovieName, movieTorrent.MovieYear, movieTorrent.MovieImdbId, "Cinematic Cut", Path.GetExtension(movieTorrent.Path).Replace(".", ""));
        //     List<Application.MovieStorageLocationRecord> storageLocationRecords = await Application.instance.GetMovieStorageLocationRecordsAsync(movieTorrent.MovieId);
        //     foreach (var storageLocationRecord in storageLocationRecords)
        //     {
        //         using var db = Db.CreateConnection();
        //         using var transaction = db.Connection.BeginTransaction();
        //         await db
        //             .Query("MovieStorageLocationFile")
        //             .InsertAsync(new {
        //                 MovieTorrentFileId = movieTorrent.Id,
        //                 MovieStorageLocationId = storageLocationRecord.Id,
        //                 FilePath = path
        //             });
        //         IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocationRecord.StorageLocation);
        //         if (storage == null)
        //         {
        //             continue;
        //         }
        //         contents.Seek(0, SeekOrigin.Begin);
        //         await storage.SaveFileByPathAsync(path, contents);
        //         transaction.Commit();
        //     }
        //     await torrentClient.RemoveTorrentByHashAsync(movieTorrent.Hash);
        // }
    }
}