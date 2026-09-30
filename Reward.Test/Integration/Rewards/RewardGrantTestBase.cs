namespace Reward.Test.Integration.Rewards;

[Collection(RewardGrantCollection.Name)]
public abstract class RewardGrantTestBase(RewardGrantFixture fixture) : IAsyncLifetime
{
    protected RewardGrantFixture Fixture { get; } = fixture;

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
