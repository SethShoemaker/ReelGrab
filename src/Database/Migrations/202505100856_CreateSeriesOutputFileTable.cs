using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateSeriesOutputFileTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE SeriesOutputFile(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeriesId INTEGER NOT NULL REFERENCES Series(Id),
                EpisodeId INTEGER NOT NULL REFERENCES SeriesEpisode(Id),
                SeriesTorrentMappingId INTEGER REFERENCES SeriesTorrentMapping(Id),
                SeriesStorageLocationId INTEGER REFERENCES SeriesStorageLocation(Id),
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
            DROP TABLE SeriesOutputFile;
        ");
    }
}