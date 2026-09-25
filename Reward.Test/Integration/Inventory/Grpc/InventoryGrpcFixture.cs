using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Reward.Contracts.V1;

namespace Reward.Test.Integration.Inventory.Grpc;

public sealed class InventoryGrpcFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_inventory_grpc";
    private TestDatabase? database;
    private RewardWebApplicationFactory? factory;
    private GrpcChannel? channel;

    public IServiceProvider Services => factory?.Services ?? throw new InvalidOperationException("Fixture not initialized.");

    public RewardInventoryService.RewardInventoryServiceClient CreateClient()
    {
        TestServer server = factory?.Server ?? throw new InvalidOperationException("Fixture not initialized.");
        channel ??= GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = server.CreateHandler()
        });

        return new RewardInventoryService.RewardInventoryServiceClient(channel);
    }

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync(DatabaseName);
        factory = new RewardWebApplicationFactory(database.ConnectionString);
        await database.ResetAsync();
    }

    public Task ResetAsync() => database?.ResetAsync() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }
}
