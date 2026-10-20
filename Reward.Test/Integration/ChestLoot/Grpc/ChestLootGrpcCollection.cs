namespace Reward.Test.Integration.ChestLoot.Grpc;

[CollectionDefinition(Name)]
public sealed class ChestLootGrpcCollection : ICollectionFixture<ChestLootGrpcFixture>
{
    public const string Name = "Chest loot gRPC integration tests";
}
