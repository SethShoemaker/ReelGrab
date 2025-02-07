using SqlKata.Execution;

namespace ReelGrab.Core.Migrations;

public class CreateStorageGatewayConfigTable : Migration
{
    public override async Task Up(QueryFactory db)
    {
        await db.StatementAsync(
            "CREATE TABLE StorageGatewayConfig (" +
            "Key VARCHAR(256) NOT NULL PRIMARY KEY," +
            "Value VARCHAR(256) );"
        );
    }

    public override async Task Down(QueryFactory db)
    {
        await db.StatementAsync(
            "DROP TABLE StorageGatewayConfig;"
        );
    }
}