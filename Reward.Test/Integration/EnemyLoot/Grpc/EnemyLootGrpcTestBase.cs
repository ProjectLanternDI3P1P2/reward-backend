namespace Reward.Test.Integration.EnemyLoot.Grpc;

/// <summary>Provides isolated database state for enemy loot gRPC integration tests.</summary>
[Collection(EnemyLootGrpcCollection.Name)]
public abstract class EnemyLootGrpcTestBase(EnemyLootGrpcFixture fixture) : IAsyncLifetime
{
    protected EnemyLootGrpcFixture Fixture { get; } = fixture;

    /// <summary>Resets persisted test data before each integration test.</summary>
    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    /// <summary>Completes test cleanup without asynchronous resources.</summary>
    public ValueTask DisposeAsync()
    {
        // A derived test class may add a finalizer later, so the base class suppresses it.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
