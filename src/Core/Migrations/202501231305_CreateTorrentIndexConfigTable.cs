using SqlKata.Execution;

namespace ReelGrab.Core.Migrations;

public class CreateTorrentIndexConfigTable : Migration
{
    public async override Task Up(QueryFactory db)
    {
        await db.StatementAsync(
           "CREATE TABLE TorrentIndexConfig (" +
           "Key VARCHAR(256) NOT NULL PRIMARY KEY," +
           "Value VARCHAR(256) );"
       );
    }

    public async override Task Down(QueryFactory db)
    {
        await db.StatementAsync(
            "DROP TABLE TorrentIndexConfig;"
        );
    }
}