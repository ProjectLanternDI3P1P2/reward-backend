using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Reward.Contracts.V1;

namespace Reward.Test.Integration.ChestLoot.Grpc;

public sealed class ChestLootGrpcFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_chest_loot_grpc";
    private TestDatabase? database;
    private RewardWebApplicationFactory? factory;
    private GrpcChannel? channel;

    public IServiceProvider Services =>
        factory?.Services ?? throw new InvalidOperationException("Fixture not initialized.");

    public RewardLootService.RewardLootServiceClient CreateClient()
    {
        // Reuse one in-memory HTTP/2 channel while creating independent typed clients as needed.
        TestServer server =
            factory?.Server ?? throw new InvalidOperationException("Fixture not initialized.");
        channel ??= GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = server.CreateHandler() }
        );
        return new RewardLootService.RewardLootServiceClient(channel);
    }

    public async ValueTask InitializeAsync()
    {
        // Give this collection an isolated migrated PostgreSQL database.
        database = await TestDatabase.CreateAsync(DatabaseName);
        factory = new RewardWebApplicationFactory(database.ConnectionString);
        await database.ResetAsync();
    }

    public Task ResetAsync() => database?.ResetAsync() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        // Release the transport and application before dropping their database.
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }
}
