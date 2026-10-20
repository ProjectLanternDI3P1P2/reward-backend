namespace Reward.Test.Integration.Chests.Controllers;

[CollectionDefinition(Name)]
public sealed class ChestControllerCollection : ICollectionFixture<ChestControllerFixture>
{
    public const string Name = "Chest controller integration tests";
}
