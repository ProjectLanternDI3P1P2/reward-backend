namespace Reward.Test.Integration.Rewards;

[CollectionDefinition(Name)]
public sealed class RewardGrantCollection : ICollectionFixture<RewardGrantFixture>
{
    public const string Name = "Reward grant integration tests";
}
