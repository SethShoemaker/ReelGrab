using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class AddMovieTorrents : Job
{
    private static string TorrentFileDir = "/app/torrents";

    public override TimeSpan Interval => TimeSpan.FromSeconds(15);

    public record MovieTorrent(int MovieId, string MovieImdbId, string LocalPath, string Hash, string Path);

    public record Row(long MovieId, string MovieImdbId, long TorrentId, string Hash, string Path);

    public async Task<List<MovieTorrent>> GetMovieTorrentsAsync(CancellationToken stoppingToken)
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Movie")
            .Join("MovieTorrent", j => j.On("Movie.Id", "MovieTorrent.MovieId"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Join("MovieTorrentFile", j => j.On("MovieTorrent.Id", "MovieTorrentFile.MovieTorrentId"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Where("MovieTorrentFile.Name", "Theatrical Release")
            .Select(["Movie.Id AS MovieId", "Movie.ImdbId AS MovieImdbId", "Torrent.Id AS TorrentId", "Torrent.Hash", "TorrentFile.Path"])
            .GetAsync<Row>(cancellationToken: stoppingToken);
        return rows
            .Select(r => new MovieTorrent(
                MovieId: (int)r.MovieId,
                MovieImdbId: r.MovieImdbId,
                LocalPath: Path.Join(TorrentFileDir, $"{r.TorrentId}.torrent"),
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
            if (storage != null && await storage.HasMovieSavedAsync(movieId, "Theatrical Release"))
            {
                return true;
            }
        }
        return false;
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        using var db = Db.CreateConnection();
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
                await torrentClient.ProvisionTorrentByLocalPathAsync(movieTorrent.LocalPath);
                await torrentClient.SetAllTorrentFilesAsNotWantedByHashAsync(movieTorrent.Hash);
            }
            List<ITorrentClient.TorrentFileInfo> torrentFiles = await torrentClient.GetTorrentFilesByHashAsync(movieTorrent.Hash);
            ITorrentClient.TorrentFileInfo? torrentFile = torrentFiles.FirstOrDefault(tf => tf.Path == movieTorrent.Path);
            if (torrentFile == null)
            {
                continue;
            }
            if(!torrentFile.Wanted)
            {
                await torrentClient.SetTorrentFilesAsWantedByHashAsync(movieTorrent.Hash, [torrentFile.Number]);
            }
            await torrentClient.StartTorrentByHashAsync(movieTorrent.Hash);
        }
    }
}