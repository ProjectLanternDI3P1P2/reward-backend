namespace Reward.Test.Integration.EnemyLoot.Grpc;

[CollectionDefinition(Name)]
public sealed class EnemyLootGrpcCollection : ICollectionFixture<EnemyLootGrpcFixture>
{
    public const string Name = "Enemy loot gRPC integration tests";
}
