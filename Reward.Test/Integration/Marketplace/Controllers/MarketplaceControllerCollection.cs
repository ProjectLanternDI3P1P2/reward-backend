namespace Reward.Test.Integration.Marketplace.Controllers;

[CollectionDefinition(Name)]
public sealed class MarketplaceControllerCollection
    : ICollectionFixture<MarketplaceControllerFixture>
{
    public const string Name = "Marketplace controller integration tests";
}
