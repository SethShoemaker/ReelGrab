using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateMovieTables : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE Movie(
                Id INTEGER NOT NULL PRIMARY KEY,
                ImdbId VARCHAR(256) NOT NULL UNIQUE,
                Name VARCHAR(256) NOT NULL,
                Description VARCHAR(256),
                Poster VARCHAR(256),
                Year INTEGER
            );

            CREATE TABLE MovieTorrent(
                Id INTEGER NOT NULL PRIMARY KEY,
                MovieId INTEGER NOT NULL REFERENCES Movie(Id),
                TorrentId INTEGER NOT NULL REFERENCES Torrent(Id)
            );

            CREATE TABLE MovieTorrentFile(
                Id INTEGER NOT NULL PRIMARY KEY,
                MovieTorrentId INTEGER NOT NULL REFERENCES MovieTorrent(Id),
                TorrentFileId INTEGER NOT NULL REFERENCES TorrentFile(Id),
                Name VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE MovieTorrentFile;
            DROP TABLE MovieTorrent;
            DROP TABLE Movie;
        ");
    }
}