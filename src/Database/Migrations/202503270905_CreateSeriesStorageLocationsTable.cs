using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateSeriesStorageLocationsTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE SeriesStorageLocation(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeriesId INTEGER NOT NULL REFERENCES Series(Id),
                StorageLocation VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE SeriesStorageLocation;
        ");
    }
}