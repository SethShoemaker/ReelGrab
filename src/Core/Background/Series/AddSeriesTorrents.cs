using ReelGrab.Database;
using ReelGrab.Storage;
using ReelGrab.Storage.Locations;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Series;

public class AddSeriesTorrents : Job
{
    private static string TorrentFileDir = "/app/torrents";

    public override TimeSpan Interval => TimeSpan.FromSeconds(15);

    public record SeriesTorrentMapping(string TorrentPath, int SeasonId, int EpisodeId);

    public record SeriesTorrent(int Id, string LocalPath, string Hash, List<SeriesTorrentMapping> Mappings);

    public record Series(int Id, string ImdbId, List<SeriesTorrent> Torrents);

    public record Row(long SeriesId, string SeriesImdbId, long TorrentId, string TorrentHash, string Path, long SeasonId, long EpisodeId);

    public async Task<List<Series>> GetSeriesAsync(CancellationToken cancellationToken)
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
            .Select(["Series.Id AS SeriesId", "Series.ImdbId AS SeriesImdbId", "Torrent.Id AS TorrentId", "Torrent.Hash AS TorrentHash", "TorrentFile.Path", "SeriesSeason.Id AS SeasonId", "SeriesEpisode.Id AS EpisodeId"])
            .GetAsync<Row>(cancellationToken: cancellationToken);
        List<Series> res = [];
        foreach (var row in rows)
        {
            Series? series = res.FirstOrDefault(s => s.Id == (int)row.SeriesId);
            if (series == null)
            {
                res.Add(series = new((int)row.SeriesId, row.SeriesImdbId, []));
            }
            SeriesTorrent? torrent = series.Torrents.FirstOrDefault(t => t.Id == (int)row.TorrentId);
            if (torrent == null)
            {
                series.Torrents.Add(torrent = new((int)row.TorrentId, Path.Join(TorrentFileDir, $"{row.TorrentId}.torrent"), row.TorrentHash, []));
            }
            torrent.Mappings.Add(new(row.Path, (int)row.SeasonId, (int)row.EpisodeId));
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
                foreach (var mapping in torrent.Mappings)
                {
                    if (await EpisodeIsSavedAnywhere(mapping.EpisodeId, storageLocations))
                    {
                        continue;
                    }
                    if (!await torrentClient.HasTorrentByHashAsync(torrent.Hash))
                    {
                        await torrentClient.ProvisionTorrentByLocalPathAsync(torrent.LocalPath);
                        await torrentClient.SetAllTorrentFilesAsNotWantedByHashAsync(torrent.Hash);
                    }
                    List<ITorrentClient.TorrentFileInfo> torrentFiles = await torrentClient.GetTorrentFilesByHashAsync(torrent.Hash);
                    ITorrentClient.TorrentFileInfo? torrentFile = torrentFiles.FirstOrDefault(tf => tf.Path == mapping.TorrentPath);
                    if (torrentFile == null)
                    {
                        continue;
                    }
                    if (!torrentFile.Wanted)
                    {
                        await torrentClient.SetTorrentFilesAsWantedByHashAsync(torrent.Hash, [torrentFile.Number]);
                    }
                    await torrentClient.StartTorrentByHashAsync(torrent.Hash);
                }
            }
        }
    }
}