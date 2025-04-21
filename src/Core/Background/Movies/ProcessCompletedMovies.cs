using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class ProcessCompletedMovies : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(60);

    public record MovieTorrent(int MovieId, string MovieImdbId, string TorrentName, string Hash, string Path);

    public record Row(long MovieId, string MovieImdbId, string TorrentName, string Hash, string Path);

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
            .Select(["Movie.Id AS MovieId", "Movie.ImdbId AS MovieImdbId", "Torrent.Name AS TorrentName", "Torrent.Hash", "TorrentFile.Path"])
            .GetAsync<Row>(cancellationToken: stoppingToken);
        return rows
            .Select(r => new MovieTorrent(
                MovieId: (int)r.MovieId,
                MovieImdbId: r.MovieImdbId,
                TorrentName: r.TorrentName,
                Hash: r.Hash,
                Path: r.Path
            ))
            .ToList();
    }

    public async Task<bool> MovieIsSavedSomewhereAsync(int movieId)
    {
        foreach (var storageLocation in await Application.instance.GetMovieStorageLocationsAsync(movieId))
        {
            IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocation);
            if (storage != null && await storage.HasMovieSavedAsync(movieId, "Cinematic Cut"))
            {
                return true;
            }
        }
        return false;
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        TorrentClient torrentClient = TorrentClient.instance;
        if (!torrentClient.Implemented || !await torrentClient.ConnectionGoodAsync())
        {
            return;
        }
        List<MovieTorrent> movieTorrents = await GetMovieTorrentsAsync(stoppingToken);
        foreach (var movieTorrent in movieTorrents)
        {
            if (await MovieIsSavedSomewhereAsync(movieTorrent.MovieId))
            {
                continue;
            }
            if (!await torrentClient.HasTorrentByHashAsync(movieTorrent.Hash))
            {
                continue;
            }
            ITorrentClient.TorrentFileInfo? torrentFile = (await torrentClient.GetTorrentFilesByHashAsync(movieTorrent.Hash)).FirstOrDefault(tf => tf.Path == movieTorrent.Path);
            if (torrentFile == null)
            {
                continue;
            }
            if (torrentFile.Progress < 100)
            {
                continue;
            }
            Stream contents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFilePathAsync(movieTorrent.Hash, movieTorrent.Path);
            List<string> storageLocations = await Application.instance.GetMovieStorageLocationsAsync(movieTorrent.MovieId);
            foreach (var storageLocation in storageLocations)
            {
                IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocation);
                if (storage == null || await storage.HasMovieSavedAsync(movieTorrent.MovieId, "Cinematic Cut"))
                {
                    continue;
                }
                contents.Seek(0, SeekOrigin.Begin);
                await storage.SaveMovieAsync(movieTorrent.MovieId, "Cinematic Cut", Path.GetExtension(movieTorrent.Path), contents);
            }
            await torrentClient.RemoveTorrentByHashAsync(movieTorrent.Hash);
        }
    }
}