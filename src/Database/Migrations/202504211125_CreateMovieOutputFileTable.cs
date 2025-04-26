using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateMovieOutputFileTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE MovieOutputFile(
                Id INTEGER NOT NULL PRIMARY KEY,
                MovieId INTEGER NOT NULL REFERENCES Movie(Id),
                MovieTorrentFileId INTEGER REFERENCES MovieTorrentFile(Id),
                MovieStorageLocationId INTEGER REFERENCES MovieStorageLocation(Id),
                StorageLocation VARCHAR(256) NOT NULL,
                Name VARCHAR(256) NOT NULL,
                FilePath VARCHAR(256) NOT NULL,
                Status VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE MovieOutputFile;
        ");
    }
}