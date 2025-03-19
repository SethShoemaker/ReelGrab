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

    private static readonly List<Type> Migrations = [
        typeof(CreateTorrentTables),
        typeof(CreateMovieTables),
        typeof(CreateSeriesTables)
    ];

    public static async Task ApplyMigrationsAsync()
    {
        using QueryFactory db = CreateConnection();
        await EnsureMigrationsTableExistsAsync(db);
        foreach(var migration in await GetPendingMigrationTypesAsync(db)){
            await ApplyMigration(migration);
        }
    }

    public static async Task ApplyMigration(Type migration)
    {
        if (!typeof(Migration).IsAssignableFrom(migration))
        {
            throw new ArgumentException($"Type {migration.Name} must inherit from Migration");
        }
        using var db = CreateConnection();
        Migration instance = (Migration)(Activator.CreateInstance(migration) ?? throw new Exception($"could not instantiate {migration.FullName}"));
        Console.WriteLine($"About to run {migration.Name}");
        using var transaction = db.Connection.BeginTransaction();
        try
        {
            await instance.Up(db);
            await db.Query("Migrations").InsertAsync(new { Name = migration.FullName, Timestamp = DateTime.Now });
            transaction.Commit();
            Console.WriteLine($"${migration.Name} ran successfully, saving migration record");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task<List<Type>> GetPendingMigrationTypesAsync(QueryFactory db)
    {
        string? lastMigration = await db
            .Query("Migrations")
            .Select("Name")
            .OrderByDesc("Timestamp")
            .FirstOrDefaultAsync<string>();
        if(lastMigration == null){
            return Migrations;
        }
        if(!Migrations.Select(t => t.FullName).Contains(lastMigration)){
            throw new InvalidDataException($"could not find migration {lastMigration}");
        }
        return Migrations.SkipWhile(m => m.FullName != lastMigration).Skip(1).ToList();
    }

    private static async Task EnsureMigrationsTableExistsAsync(QueryFactory db)
    {
        await db.StatementAsync(
            "CREATE TABLE IF NOT EXISTS Migrations(" + 
            "Name VARCHAR(256) NOT NULL PRIMARY KEY," +
            "Timestamp DATETIME NOT NULL);"
        );
    }
}