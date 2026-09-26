namespace Reward.Test.Integration.Placeholders.Controllers;

[CollectionDefinition(Name)]
public sealed class PlaceholdersControllerCollection
    : ICollectionFixture<PlaceholdersControllerFixture>
{
    public const string Name = "Placeholders controller integration tests";
}
