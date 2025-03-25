using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class AddWantedFlagsToMovieAndSeriesTables : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            ALTER TABLE Movie ADD COLUMN Wanted INTEGER NOT NULL CHECK (Wanted IN (0, 1));
            ALTER TABLE SeriesEpisode ADD COLUMN Wanted INTEGER NOT NULL CHECK (Wanted IN (0, 1));
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.SelectAsync(@"
            ALTER TABLE SeriesEpisode DROP COLUMN Wanted;
            ALTER TABLE Movie DROP COLUMN Wanted;
        ");
    }
}