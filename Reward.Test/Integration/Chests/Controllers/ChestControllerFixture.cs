namespace Reward.Test.Integration.Chests.Controllers;

public sealed class ChestControllerFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_chest";
    private TestDatabase? database;
    private RewardWebApplicationFactory? factory;

    public IServiceProvider Services =>
        factory?.Services ?? throw new InvalidOperationException("Fixture not initialized.");
    public HttpClient HttpClient =>
        factory?.CreateClient() ?? throw new InvalidOperationException("Fixture not initialized.");

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync(DatabaseName);
        factory = new RewardWebApplicationFactory(database.ConnectionString);
        await database.ResetAsync();
    }

    public Task ResetAsync() => database?.ResetAsync() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        factory?.Dispose();
        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }
}
