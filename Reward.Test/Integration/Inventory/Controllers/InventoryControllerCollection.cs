namespace Reward.Test.Integration.Inventory.Controllers;

[CollectionDefinition(Name)]
public sealed class InventoryControllerCollection : ICollectionFixture<InventoryControllerFixture>
{
    public const string Name = "Inventory controller integration tests";
}
