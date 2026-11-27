using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reward.Application.Exceptions;
using Reward.Application.Ports;

namespace Reward.Test.Integration.Rewards;

public sealed class RewardGrantFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_rewards";
    internal static readonly Guid ViewerPlayerId = Guid.NewGuid();
    private TestDatabase? database;
    private RewardWebApplicationFactory? factory;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>? configuredFactory;

    public IServiceProvider Services =>
        configuredFactory?.Services
        ?? throw new InvalidOperationException("Fixture not initialized.");
    public HttpClient HttpClient =>
        configuredFactory?.CreateClient()
        ?? throw new InvalidOperationException("Fixture not initialized.");

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync(DatabaseName);
        factory = new RewardWebApplicationFactory(database.ConnectionString);
        configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.Replace(
                    ServiceDescriptor.Scoped<
                        IPlayerParticipationClient,
                        FakePlayerParticipationClient
                    >()
                )
            )
        );
        await database.ResetAsync();
    }

    public Task ResetAsync() => database?.ResetAsync() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        configuredFactory?.Dispose();
        factory?.Dispose();
        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }

    // Stands in for Player's RewardPlayerService, keyed by the trusted gateway identity.
    private sealed class FakePlayerParticipationClient : IPlayerParticipationClient
    {
        public Task<RunParticipation> GetRunParticipationAsync(
            string externalUserId,
            Guid dungeonRunId,
            CancellationToken cancellationToken
        ) =>
            externalUserId switch
            {
                "participant" => Task.FromResult(new RunParticipation(true, ViewerPlayerId)),
                "outsider" => Task.FromResult(new RunParticipation(false, null)),
                _ => throw new ExternalServiceUnavailableException(
                    "Player",
                    new TimeoutException("Player did not answer.")
                ),
            };
    }
}
