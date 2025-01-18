using SqlKata.Execution;

namespace ReelGrab.Core.Migrations;

public abstract class Migration
{
    public abstract Task Up(QueryFactory db);

    public abstract Task Down(QueryFactory db);
}