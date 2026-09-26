namespace Reward.Test.Integration.Placeholders.Controllers;

public sealed class PlaceholdersControllerFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_placeholders";
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
