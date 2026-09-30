namespace Reward.Test.Integration.Chests.Controllers;

[Collection(ChestControllerCollection.Name)]
public abstract class ChestControllerTestBase(ChestControllerFixture fixture) : IAsyncLifetime
{
    protected ChestControllerFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
