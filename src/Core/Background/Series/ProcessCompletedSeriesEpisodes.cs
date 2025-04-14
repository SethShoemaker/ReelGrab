using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Series;

public class ProcessCompletedSeriesEpisodes : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(60);

    public record SeriesTorrentMapping(int EpisodeId, string Path);

    public record SeriesTorrent(int Id, string Hash, List<SeriesTorrentMapping> Mappings);

    public record Series(int Id, List<SeriesTorrent> Torrents);

    private record Row(long SeriesId, long EpisodeId, string Path, long TorrentId, string TorrentHash);

    public async Task<List<Series>> GetSeriesAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Series")
            .Join("SeriesSeason", j => j.On("Series.Id", "SeriesSeason.SeriesId"))
            .Join("SeriesEpisode", j => j.On("SeriesSeason.Id", "SeriesEpisode.SeasonId"))
            .Join("SeriesTorrentMapping", j => j.On("SeriesEpisode.Id", "SeriesTorrentMapping.EpisodeId"))
            .Join("TorrentFile", j => j.On("SeriesTorrentMapping.TorrentFileId", "TorrentFile.Id"))
            .Join("SeriesTorrent", j => j.On("SeriesTorrentMapping.SeriesTorrentId", "SeriesTorrent.Id"))
            .Join("Torrent", j => j.On("SeriesTorrent.TorrentId", "Torrent.Id"))
            .Select(["Series.Id AS SeriesId", "SeriesEpisode.Id AS EpisodeId", "TorrentFile.Path", "Torrent.Id AS TorrentId", "Torrent.Hash AS TorrentHash"])
            .Where("SeriesTorrentMapping.name", "Original Broadcast")
            .GetAsync<Row>();
        List<Series> res = [];
        foreach (var row in rows)
        {
            Series? series = res.FirstOrDefault(s => s.Id == (int)row.SeriesId);
            if (series == null)
            {
                res.Add(series = new((int)row.SeriesId, []));
            }
            SeriesTorrent? torrent = series.Torrents.FirstOrDefault(t => t.Id == (int)row.TorrentId);
            if (torrent == null)
            {
                series.Torrents.Add(torrent = new((int)row.TorrentId, row.TorrentHash, []));
            }
            torrent.Mappings.Add(new((int)row.EpisodeId, row.Path));
        }
        return res;
    }

    public async Task<bool> EpisodeIsSavedAnywhere(int episodeId, List<string> storageLocations)
    {
        foreach (var storageLocation in storageLocations)
        {
            IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocation);
            if (storage != null && await storage.HasSeriesEpisodeAsync(episodeId, "Original Broadcast"))
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
        var serieses = await GetSeriesAsync(stoppingToken);
        foreach (var series in serieses)
        {
            List<string> storageLocations = await Application.instance.GetSeriesStorageLocationsAsync(series.Id);
            foreach (var torrent in series.Torrents)
            {
                if (!await torrentClient.HasTorrentByHashAsync(torrent.Hash))
                {
                    continue;
                }
                List<ITorrentClient.TorrentFileInfo> torrentFiles = await torrentClient.GetTorrentFilesByHashAsync(torrent.Hash);
                foreach (var mapping in torrent.Mappings)
                {
                    if (await EpisodeIsSavedAnywhere(mapping.EpisodeId, storageLocations))
                    {
                        continue;
                    }
                    ITorrentClient.TorrentFileInfo? torrentFile = torrentFiles.FirstOrDefault(tf => tf.Path == mapping.Path);
                    if (torrentFile == null || !torrentFile.Wanted || torrentFile.Progress < 100)
                    {
                        continue;
                    }
                    Stream contents = await torrentClient.GetCompletedTorrentFileContentsByHashAndFilePathAsync(torrent.Hash, mapping.Path);
                    foreach (var storageLocation in storageLocations)
                    {
                        IStorageLocation? storage = StorageGateway.instance.StorageLocations.FirstOrDefault(sl => sl.Id == storageLocation);
                        if (storage == null || await storage.HasSeriesEpisodeAsync(mapping.EpisodeId, "Original Broadcast"))
                        {
                            continue;
                        }
                        contents.Seek(0, SeekOrigin.Begin);
                        await storage.SaveSeriesEpisodeAsync(mapping.EpisodeId, "Original Broadcast", Path.GetExtension(mapping.Path), contents);
                    }
                    await torrentClient.SetTorrentFilesAsNotWantedByHashAsync(torrent.Hash, [torrentFile.Number]);
                    torrentFiles = await torrentClient.GetTorrentFilesByHashAsync(torrent.Hash);
                    if (torrentFiles.All(tf => !tf.Wanted))
                    {
                        await torrentClient.RemoveTorrentByHashAsync(torrent.Hash);
                    }
                }
            }
        }
    }
}
