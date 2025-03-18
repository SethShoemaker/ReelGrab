using Microsoft.Data.Sqlite;
using ReelGrab.Database.Migrations;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace ReelGrab.Database;

public class Db
{
    public static QueryFactory CreateConnection(){
        SqliteConnection connection = new("Data Source=/data/reelgrab.db");
        connection.Open();
        return new(connection, new SqliteCompiler());
    }
}