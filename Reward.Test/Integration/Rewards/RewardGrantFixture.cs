using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Reward.Presentation.Controllers;

namespace Reward.Test.Integration.Rewards;

public sealed class RewardGrantFixture : IAsyncLifetime
{
    private const string DatabaseName = "reward_test_rewards";
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
                services
                    .AddHttpClient(RunRewardsController.PlayerMembershipClient)
                    .ConfigurePrimaryHttpMessageHandler(() => new PlayerMembershipHandler())
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

    private sealed class PlayerMembershipHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string identity = request.Headers.GetValues("X-User-Id").Single();
            var status = identity switch
            {
                "participant" => HttpStatusCode.NoContent,
                "outsider" => HttpStatusCode.Forbidden,
                _ => HttpStatusCode.ServiceUnavailable,
            };
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
