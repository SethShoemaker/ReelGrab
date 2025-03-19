using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateTorrentTables : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE Torrent(
                Id INTEGER NOT NULL PRIMARY KEY,
                Url VARCHAR(256) NOT NULL UNIQUE,
                Hash VARCHAR(256) NOT NULL,
                Name VARCHAR(256) NOT NULL,
                Source VARCHAR(256) NOT NULL
            );

            CREATE TABLE TorrentFile(
                Id INTEGER NOT NULL PRIMARY KEY,
                TorrentId INTEGER NOT NULL REFERENCES Torrent(Id),
                Path VARCHAR(256) NOT NULL,
                Bytes INTEGER NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE TorrentFile;
            DROP TABLE Torrent;
        ");
    }
}