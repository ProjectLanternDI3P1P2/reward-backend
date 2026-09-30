namespace Reward.Test.Integration.ChestLoot.Grpc;

[Collection(ChestLootGrpcCollection.Name)]
public abstract class ChestLootGrpcTestBase(ChestLootGrpcFixture fixture) : IAsyncLifetime
{
    protected ChestLootGrpcFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
