namespace Reward.Test.Integration.ChestLoot.Grpc;

/// <summary>Provides isolated database state for chest loot gRPC integration tests.</summary>
[Collection(ChestLootGrpcCollection.Name)]
public abstract class ChestLootGrpcTestBase(ChestLootGrpcFixture fixture) : IAsyncLifetime
{
    protected ChestLootGrpcFixture Fixture { get; } = fixture;

    /// <summary>Resets persisted test data before each integration test.</summary>
    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    /// <summary>Completes test cleanup without asynchronous resources.</summary>
    public ValueTask DisposeAsync()
    {
        // Record that cleanup is complete even if a subclass later introduces a finalizer.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
