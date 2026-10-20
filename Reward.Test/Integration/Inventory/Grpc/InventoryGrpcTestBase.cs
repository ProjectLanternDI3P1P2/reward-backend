namespace Reward.Test.Integration.Inventory.Grpc;

[Collection(InventoryGrpcCollection.Name)]
public abstract class InventoryGrpcTestBase(InventoryGrpcFixture fixture) : IAsyncLifetime
{
    protected InventoryGrpcFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
