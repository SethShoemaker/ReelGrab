using ReelGrab.Database;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Movies;

public class AddNeededTorrentFiles : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(30);

    public record TorrentFile(int TorrentId, string TorrentHash, string Path);

    private record GetNeededTorrentFilesRow(long TorrentId, string TorrentHash, string Path);

    public async Task<List<TorrentFile>> GetNeededTorrentFiles(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        return (await db
            .Query("MovieOutputFile")
            .Join("MovieTorrentFile", j => j.On("MovieOutputFile.MovieTorrentFileId", "MovieTorrentFile.Id"))
            .Join("TorrentFile", j => j.On("MovieTorrentFile.TorrentFileId", "TorrentFile.Id"))
            .Join("MovieTorrent", j => j.On("MovieTorrentFile.MovieTorrentId", "MovieTorrent.Id"))
            .Join("Torrent", j => j.On("MovieTorrent.TorrentId", "Torrent.Id"))
            .Distinct()
            .Select(["Torrent.Id AS TorrentId", "Torrent.Hash AS TorrentHash", "TorrentFile.Path"])
            .GroupBy(["Torrent.Id", "Torrent.Hash", "TorrentFile.Path"])
            .HavingRaw("SUM(CASE WHEN MovieOutputFile.Status = 'StalePendingUpdate' OR MovieOutputFile.Status = 'InitializedPendingCreation' THEN 1 ELSE 0 END) > 0")
            .GetAsync<GetNeededTorrentFilesRow>(cancellationToken: cancellationToken))
            .Select(r => new TorrentFile(
                TorrentId: (int)r.TorrentId,
                TorrentHash: r.TorrentHash,
                Path: r.Path
            ))
            .ToList();
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        TorrentClient torrentClient = TorrentClient.instance;
        if (!torrentClient.Implemented || !await torrentClient.ConnectionGoodAsync())
        {
            return;
        }
        foreach(var torrentFile in await GetNeededTorrentFiles(stoppingToken))
        {
            if (!await torrentClient.HasTorrentByHashAsync(torrentFile.TorrentHash))
            {
                await torrentClient.ProvisionTorrentByLocalPathAsync($"/data/torrents/{torrentFile.TorrentId}.torrent");
                await torrentClient.SetAllTorrentFilesAsNotWantedByHashAsync(torrentFile.TorrentHash);
            }
            ITorrentClient.TorrentFileInfo? fileInfo = (await torrentClient
                .GetTorrentFilesByHashAsync(torrentFile.TorrentHash))
                .FirstOrDefault(tf => tf.Path == torrentFile.Path);
            if (fileInfo == null)
            {
                continue;
            }
            if(!fileInfo.Wanted)
            {
                await torrentClient.SetTorrentFilesAsWantedByHashAsync(torrentFile.TorrentHash, [fileInfo.Number]);
            }
            await torrentClient.StartTorrentByHashAsync(torrentFile.TorrentHash);
        }
    }
}