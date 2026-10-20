namespace Reward.Test.Integration.Chests.Controllers;

[Collection(ChestControllerCollection.Name)]
public abstract class ChestControllerTestBase(ChestControllerFixture fixture) : IAsyncLifetime
{
    protected ChestControllerFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync()
    {
        // A derived test class may add a finalizer later, so the base class suppresses it.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
