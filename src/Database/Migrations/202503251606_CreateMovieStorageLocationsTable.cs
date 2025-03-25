using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateMovieStorageLocationsTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE MovieStorageLocation(
                Id INTEGER NOT NULL PRIMARY KEY,
                MovieId INTEGER NOT NULL REFERENCES Movie(Id),
                StorageLocation VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE MovieStorageLocation;
        ");
    }
}