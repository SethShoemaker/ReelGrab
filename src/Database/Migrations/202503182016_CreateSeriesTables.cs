using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public class CreateSeriesTables : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE Series(
                Id INTEGER NOT NULL PRIMARY KEY,
                ImdbId VARCHAR(256) NOT NULL UNIQUE,
                Name VARCHAR(256) NOT NULL,
                Description VARCHAR(256),
                Poster VARCHAR(256),
                StartYear INTEGER NOT NULL,
                EndYear INTEGER
            );

            CREATE TABLE SeriesSeason(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeriesId INTEGER NOT NULL REFERENCES Series(Id),
                Number INTEGER NOT NULL,
                Description VARCHAR(256),
                Poster VARCHAR(256)
            );

            CREATE TABLE SeriesEpisode(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeasonId INTEGER NOT NULL REFERENCES SeriesSeason(Id),
                Number INTEGER NOT NULL,
                ImdbId VARCHAR(256) NOT NULL,
                Name VARCHAR(256) NOT NULL,
                Description VARCHAR(256),
                Poster VARCHAR(256)
            );

            CREATE TABLE SeriesTorrent(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeriesId INTEGER NOT NULL REFERENCES Series(Id),
                TorrentId INTEGER NOT NULL REFERENCES Torrent(Id)
            );

            CREATE TABLE SeriesTorrentMapping(
                Id INTEGER NOT NULL PRIMARY KEY,
                SeriesTorrentId INTEGER NOT NULL REFERENCES SeriesTorrent(Id),
                EpisodeId INTEGER NOT NULL REFERENCES SeriesEpisode(Id),
                TorrentFileId INTEGER NOT NULL REFERENCES TorrentFile(Id),
                Name VARCHAR(256) NOT NULL
            );
        ");
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE SeriesTorrentMapping;
            DROP TABLE SeriesTorrent;
            DROP TABLE SeriesEpisode;
            DROP TABLE SeriesSeason;
            DROP TABLE Series;
        ");
    }
}