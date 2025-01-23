using Microsoft.Data.Sqlite;
using ReelGrab.Core.Migrations;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace ReelGrab.Core;

public partial class Application
{
    private readonly List<Type> Migrations = [
        typeof(CreateMediaIndexConfigTable),
        typeof(CreateStorageGatewayConfigTable),
        typeof(CreateTorrentIndexConfigTable)
    ];

    private QueryFactory Db(){
        SqliteConnection connection = new("Data Source=/data/reelgrab.db");
        connection.Open();
        return new(connection, new SqliteCompiler());
    }

    public async Task ApplyMigrationsAsync()
    {
        using QueryFactory db = Db();
        await EnsureMigrationsTableExistsAsync(db);
        foreach(var type in await GetPendingMigrationTypesAsync(db)){
            Migration migration = (Migration)(Activator.CreateInstance(type) ?? throw new Exception($"could not instantiate {type.FullName}"));
            Console.WriteLine($"About to run {type.Name}");
            await migration.Up(db);
            Console.WriteLine($"${type.Name} ran successfully, saving migration record");
            await SaveMigrationRecordAsync(type.FullName!, DateTime.Now, db);
        }
    }

    private async Task<List<Type>> GetPendingMigrationTypesAsync(QueryFactory db)
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

    private async Task EnsureMigrationsTableExistsAsync(QueryFactory db)
    {
        await db.StatementAsync(
            "CREATE TABLE IF NOT EXISTS Migrations(" + 
            "Name VARCHAR(256) NOT NULL PRIMARY KEY," +
            "Timestamp DATETIME NOT NULL);"
        );
    }

    private Task SaveMigrationRecordAsync(string name, DateTime timestamp, QueryFactory db)
    {
        return db.Query("Migrations").InsertAsync(new { name, timestamp });
    }
}