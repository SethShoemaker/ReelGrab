using Microsoft.Data.Sqlite;
using ReelGrab.Database.Migrations;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace ReelGrab.Database;

public class Db
{
    private static readonly List<Type> Migrations = [
        typeof(CreateMediaIndexConfigTable),
        typeof(CreateStorageGatewayConfigTable),
        typeof(CreateTorrentIndexConfigTable),
        typeof(CreateWantedMediaTables),
        typeof(CreateTorrentClientConfigTable)
    ];

    public static QueryFactory CreateConnection(){
        SqliteConnection connection = new("Data Source=/data/reelgrab.db");
        connection.Open();
        return new(connection, new SqliteCompiler());
    }

    public static async Task ApplyMigrationsAsync()
    {
        using QueryFactory db = CreateConnection();
        await EnsureMigrationsTableExistsAsync(db);
        foreach(var type in await GetPendingMigrationTypesAsync(db)){
            Migration migration = (Migration)(Activator.CreateInstance(type) ?? throw new Exception($"could not instantiate {type.FullName}"));
            Console.WriteLine($"About to run {type.Name}");
            await migration.Up(db);
            Console.WriteLine($"${type.Name} ran successfully, saving migration record");
            await SaveMigrationRecordAsync(type.FullName!, DateTime.Now, db);
        }
    }

    private static async Task<List<Type>> GetPendingMigrationTypesAsync(QueryFactory db)
    {
        string? className = await db.Query("Migrations").Select("Name").OrderByDesc("Timestamp").FirstOrDefaultAsync<string>();
        if(className == null){
            return Migrations;
        }
        if(!Migrations.Select(t => t.FullName).Contains(className)){
            throw new InvalidDataException($"could not find migration {className}");
        }
        return Migrations.SkipWhile(m => m.FullName != className).Skip(1).ToList();
    }

    private static async Task EnsureMigrationsTableExistsAsync(QueryFactory db)
    {
        await db.StatementAsync(
            "CREATE TABLE IF NOT EXISTS Migrations(" + 
            "Name VARCHAR(256) NOT NULL PRIMARY KEY," +
            "Timestamp DATETIME NOT NULL);"
        );
    }

    private static Task SaveMigrationRecordAsync(string name, DateTime timestamp, QueryFactory db)
    {
        return db.Query("Migrations").InsertAsync(new { name, timestamp });
    }
}