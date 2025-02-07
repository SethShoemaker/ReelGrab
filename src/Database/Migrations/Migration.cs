using SqlKata.Execution;

namespace ReelGrab.Database.Migrations;

public abstract class Migration
{
    public abstract Task Up(QueryFactory db);

    public abstract Task Down(QueryFactory db);
}