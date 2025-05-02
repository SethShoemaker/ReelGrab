using ReelGrab.Database;
using ReelGrab.TorrentClients;
using SqlKata.Execution;

namespace ReelGrab.Core.Background.Torrents;

public class SyncRequestedTorrents : Job
{
    public override TimeSpan Interval => TimeSpan.FromSeconds(30);

    public record Torrent(int Id, string Hash, List<TorrentFile> Files);

    public record TorrentFile(int Id, string Path, int NumRequests);

    private record GetTorrentsAsyncTorrentRow(long Id, string Hash, long TorrentFileId, string Path);

    public async Task<List<Torrent>> GetTorrentsAsync(CancellationToken cancellationToken)
    {
        using var db = Db.CreateConnection();
        var rows = await db
            .Query("Torrent")
            .Join("TorrentFile", j => j.On("Torrent.Id", "TorrentFile.TorrentId"))
            .Select(["Torrent.Id", "Torrent.Hash", "TorrentFile.Id AS TorrentFileId", "TorrentFile.Path"])
            .GetAsync<GetTorrentsAsyncTorrentRow>(cancellationToken: cancellationToken);
        List<Torrent> requestedTorrents = [];
        foreach (var row in rows)
        {
            Torrent? torrent = requestedTorrents.FirstOrDefault(rt => rt.Id == row.Id);
            if (torrent == null)
            {
                requestedTorrents.Add(torrent = new((int)row.Id, row.Hash, []));
            }
            torrent.Files.Add(new(
                Id: (int)row.TorrentFileId,
                Path: row.Path,
                NumRequests: await db
                    .Query("TorrentFileRequest")
                    .Where("TorrentFileId", row.TorrentFileId)
                    .CountAsync<int>()
            ));
        }
        return requestedTorrents;
    }

    public override async Task RunAsync(CancellationToken stoppingToken)
    {
        TorrentClient torrentClient = TorrentClient.instance;
        if (!torrentClient.Implemented || !await torrentClient.ConnectionGoodAsync())
        {
            return;
        }
        foreach (var torrent in await GetTorrentsAsync(stoppingToken))
        {
            if (torrent.Files.All(f => f.NumRequests == 0))
            {
                if (await torrentClient.HasTorrentByHashAsync(torrent.Hash))
                {
                    await torrentClient.RemoveTorrentByHashAsync(torrent.Hash);
                }
                continue;
            }
            if (!await torrentClient.HasTorrentByHashAsync(torrent.Hash))
            {
                await torrentClient.ProvisionTorrentByLocalPathAsync($"/data/torrents/{torrent.Id}.torrent");
                await torrentClient.SetAllTorrentFilesAsNotWantedByHashAsync(torrent.Hash);
            }
            List<ITorrentClient.TorrentFileInfo> files = await torrentClient.GetTorrentFilesByHashAsync(torrent.Hash);
            await torrentClient.SetTorrentFilesAsWantedByHashAsync(
                torrent.Hash,
                files
                    .Where(f => torrent.Files.Where(f => f.NumRequests > 0).Any(fp => fp.Path == f.Path))
                    .Where(f => !f.Wanted)
                    .Select(f => f.Number)
                    .ToList()
            );
            await torrentClient.SetTorrentFilesAsNotWantedByHashAsync(
                torrent.Hash,
                files
                    .Where(f => torrent.Files.Where(f => f.NumRequests == 0).Any(fp => fp.Path == f.Path))
                    .Where(f => f.Wanted)
                    .Select(f => f.Number)
                    .ToList()
            );
            await torrentClient.StartTorrentByHashAsync(torrent.Hash);
        }
    }
}