namespace Reward.Test.Integration.Inventory.Controllers;

[Collection(InventoryControllerCollection.Name)]
public abstract class InventoryControllerTestBase(InventoryControllerFixture fixture)
    : IAsyncLifetime
{
    protected InventoryControllerFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
