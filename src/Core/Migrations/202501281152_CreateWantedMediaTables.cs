using SqlKata.Execution;

namespace ReelGrab.Core.Migrations;

public class CreateWantedMediaTables : Migration
{
    public async override Task Up(QueryFactory db)
    {
        await db.StatementAsync(@"
            CREATE TABLE WantedMedia (
                ImdbId VARCHAR(256) NOT NULL PRIMARY KEY,
                DisplayName VARCHAR(256) NOT NULL UNIQUE,
                Description VARCHAR(256),
                PosterUrl VARCHAR(256),
                Type VARCHAR(256) NOT NULL CHECK (Type IN ('MOVIE', 'SERIES')),
                StartYear INTEGER NOT NULL,
                EndYear INTEGER
            );
        ");

        await db.StatementAsync(@"
            CREATE TABLE WantedMediaDownloadable (
                MediaId VARCHAR(256) NOT NULL REFERENCES WantedMedia(ImdbId),
                ImdbId VARCHAR(256) NOT NULL PRIMARY KEY,
                DisplayName VARCHAR(256) NOT NULL UNIQUE,
                Wanted INTEGER NOT NULL CHECK (Wanted IN (0, 1)),
                Type VARCHAR(256) NOT NULL CHECK (Type IN ('FullMovie', 'SeriesEpisode')),
                Season INTEGER NOT NULL,
                Episode INTEGER NOT NULL
            )
        ");

        await db.StatementAsync(@"
            CREATE TABLE WantedMediaTorrent(
                MediaId VARCHAR(256) NOT NULL REFERENCES WantedMedia(ImdbId),
                TorrentUrl VARCHAR(256) NOT NULL,
                Source VARCHAR(256) NOT NULL,
                DisplayName VARCHAR(256) NOT NULL,
                Hash VARCHAR(256) NOT NULL,
                UNIQUE(MediaId, TorrentUrl),
                UNIQUE(MediaId, DisplayName),
                UNIQUE(MediaId, Hash)
            );
        ");

        await db.StatementAsync(@"
            CREATE TABLE WantedMediaTorrentDownloadable(
                MediaId VARCHAR(256) NOT NULL REFERENCES WantedMedia(ImdbId),
                TorrentDisplayName VARCHAR(256) NOT NULL,
                TorrentFilePath VARCHAR(256) NOT NULL,
                DownloadableId VARCHAR(256) NOT NULL REFERENCES WantedMediaDownloadable(ImdbId),
                CONSTRAINT fk_wantedmediatorrent
                    FOREIGN KEY (MediaId, TorrentDisplayName)
                    REFERENCES WantedMediaTorrent(MediaId, DisplayName)
            );
        ");

        await db.StatementAsync(@"
            CREATE TABLE WantedMediaStorageLocation(
                MediaId VARCHAR(256) NOT NULL REFERENCES WantedMedia(ImdbId),
                StorageLocation VARCHAR(256) NOT NULL,
                PRIMARY KEY(MediaId, StorageLocation)
            )
        ");
    }

    public async override Task Down(QueryFactory db)
    {
        await db.StatementAsync(@"
            DROP TABLE WantedMediaStorageLocation;
            DROP TABLE WantedMediaTorrentDownloadable;
            DROP TABLE WantedMediaTorrent;
            DROP TABLE WantedMediaDownloadable;
            DROP TABLE WantedMedia;
        ");
    }
}