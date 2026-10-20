using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Reward.Infrastructure.Persistence;

namespace Reward.Test.Integration;

public sealed class TestDatabase : IAsyncDisposable
{
    private const string ConnectionStringEnvironmentVariable = "REWARD_TEST_DATABASE_CONNECTION";
    private readonly string databaseName;
    private readonly Respawner respawner;

    private TestDatabase(string databaseName, string connectionString, Respawner respawner)
    {
        this.databaseName = databaseName;
        ConnectionString = connectionString;
        this.respawner = respawner;
    }

    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync(string databaseName)
    {
        string adminConnectionString = GetAdminConnectionString();
        await CreateDatabaseAsync(adminConnectionString, databaseName);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName,
        };
        string connectionString = connectionStringBuilder.ConnectionString;

        var options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using (var dbContext = new RewardDbContext(options))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Respawner respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres, SchemasToInclude = ["public"] }
        );

        return new TestDatabase(databaseName, connectionString, respawner);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(GetAdminConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)} WITH (FORCE)",
            connection
        );
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateDatabaseAsync(string adminConnectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName",
            connection
        );
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);
        if (await existsCommand.ExecuteScalarAsync() is not null)
        {
            return;
        }

        await using var createCommand = new NpgsqlCommand(
            $"CREATE DATABASE {QuoteIdentifier(databaseName)}",
            connection
        );
        await createCommand.ExecuteNonQueryAsync();
    }

    private static string GetAdminConnectionString() =>
        Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
        ?? "Host=localhost;Port=5433;Database=reward;Username=reward";

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";
}
