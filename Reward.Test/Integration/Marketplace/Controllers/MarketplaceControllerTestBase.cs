namespace Reward.Test.Integration.Marketplace.Controllers;

[Collection(MarketplaceControllerCollection.Name)]
public abstract class MarketplaceControllerTestBase(MarketplaceControllerFixture fixture)
    : IAsyncLifetime
{
    protected MarketplaceControllerFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
