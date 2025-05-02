using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateTorrentFileRequestTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE TorrentFileRequest(
                Id INTEGER NOT NULL PRIMARY KEY,
                TorrentFileId INTEGER NOT NULL REFERENCES TorrentFile(Id),
                Requester VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE TorrentFileRequest;
        ");
    }
}