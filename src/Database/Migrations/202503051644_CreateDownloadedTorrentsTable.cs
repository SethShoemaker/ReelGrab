using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateDownloadedTorrentsTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE DownloadedTorrent (
                Url VARCHAR(256) NOT NULL PRIMARY KEY,
                FileGuid VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE DownloadedTorrent;
        ");
    }
}