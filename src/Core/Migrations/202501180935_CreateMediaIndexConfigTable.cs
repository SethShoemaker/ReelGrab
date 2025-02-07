using SqlKata.Execution;

namespace ReelGrab.Core.Migrations;

public class CreateMediaIndexConfigTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(
            "CREATE TABLE MediaIndexConfig (" + 
            "Key VARCHAR(256) NOT NULL PRIMARY KEY," +
            "Value VARCHAR(256) );"
        );
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(
            "DROP TABLE MediaIndexConfig;"
        );
    }
}