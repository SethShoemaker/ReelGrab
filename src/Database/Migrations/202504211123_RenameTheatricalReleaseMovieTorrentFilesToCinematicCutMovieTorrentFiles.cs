using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class RenameTheatricalReleaseMovieTorrentFilesToCinematicCutMovieTorrentFiles : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db
            .Query("MovieTorrentFile")
            .Where("Name", "Cinematic Cut")
            .UpdateAsync(new {
                Name = "Cinematic Cut"
            });
    }

    public override async Task Down(QueryFactory db)
    {
        await db
            .Query("MovieTorrentFile")
            .Where("Name", "Cinematic Cut")
            .UpdateAsync(new {
                Name = "Cinematic Cut"
            });
    }
}