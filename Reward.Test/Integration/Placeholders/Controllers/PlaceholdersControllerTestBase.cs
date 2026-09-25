namespace Reward.Test.Integration.Placeholders.Controllers;

[Collection(PlaceholdersControllerCollection.Name)]
public abstract class PlaceholdersControllerTestBase(PlaceholdersControllerFixture fixture) : IAsyncLifetime
{
    protected PlaceholdersControllerFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
