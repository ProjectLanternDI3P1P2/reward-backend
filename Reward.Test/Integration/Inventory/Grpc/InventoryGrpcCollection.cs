namespace Reward.Test.Integration.Inventory.Grpc;

[CollectionDefinition(Name)]
public sealed class InventoryGrpcCollection : ICollectionFixture<InventoryGrpcFixture>
{
    public const string Name = "Inventory gRPC integration tests";
}
