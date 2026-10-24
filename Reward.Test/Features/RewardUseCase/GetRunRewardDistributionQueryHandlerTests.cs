using FluentAssertions;
using Moq;
using Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.RewardUseCase;

public sealed class GetRunRewardDistributionQueryHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid runId = Guid.NewGuid();
    private readonly Guid bossId = Guid.NewGuid();
    private readonly Guid swordId = Guid.NewGuid();
    private readonly Guid potionId = Guid.NewGuid();
    private readonly Mock<IRewardRepository> repository = new();

    [Fact]
    public async Task Handle_DistributedRewards_ListsEveryDistributedReward()
    {
        // Arrange
        SetupRewards(
            CreateReward(RewardDistribution.Unique, bossId, swordId, "Blazing Aegis", 1),
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 2)
        );

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        result.DungeonRunId.Should().Be(runId);
        RewardDistributionCause cause = result.Distributions.Should().ContainSingle().Subject;
        cause.SourceType.Should().Be("BOSS");
        cause.CauseId.Should().Be(bossId);
        cause.Rewards.Select(reward => reward.Name).Should().Equal("Blazing Aegis", "Potion");
        cause.Rewards.Should().Contain(reward => reward.Rarity == "RARE" && reward.Quantity > 0);
    }

    [Fact]
    public async Task Handle_UniqueReward_IdentifiesTheReceivingPlayerAndHero()
    {
        // Arrange
        RewardEntity unique = CreateReward(
            RewardDistribution.Unique,
            bossId,
            swordId,
            "Blazing Aegis",
            1
        );
        SetupRewards(unique);

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        DistributedReward reward = result.Distributions.Single().Rewards.Single();
        reward.Kind.Should().Be("UNIQUE");
        reward
            .Recipients.Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new RewardRecipient(
                    unique.HeroId,
                    unique.RecipientPlayerId,
                    unique.Items.Single().ItemInstanceId
                )
            );
    }

    [Fact]
    public async Task Handle_RewardGrantedToEveryParticipant_IsListedOnceAsSharedPerParticipant()
    {
        // Arrange
        RewardEntity[] perHero =
        [
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 2),
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 2),
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 2),
        ];
        SetupRewards(perHero);

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        DistributedReward reward = result.Distributions.Single().Rewards.Single();
        reward.Kind.Should().Be("SHARED_PER_PARTICIPANT");
        reward.Quantity.Should().Be(2);
        reward
            .Recipients.Select(recipient => recipient.HeroId)
            .Should()
            .BeEquivalentTo(perHero.Select(reward => reward.HeroId));
    }

    [Fact]
    public async Task Handle_RewardsOfSeveralEvents_AreSeparatedByCauseInChronologicalOrder()
    {
        // Arrange
        Guid laterCauseId = Guid.NewGuid();
        SetupRewards(
            CreateReward(RewardDistribution.Unique, laterCauseId, swordId, "Sword", 1, minutes: 30),
            CreateReward(RewardDistribution.Unique, bossId, potionId, "Potion", 1, minutes: 5)
        );

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        result.Distributions.Select(cause => cause.CauseId).Should().Equal(bossId, laterCauseId);
    }

    [Fact]
    public async Task Handle_SameItemWithDifferentQuantities_KeepsTheRewardsApart()
    {
        // Arrange
        SetupRewards(
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 1),
            CreateReward(RewardDistribution.SharedPerParticipant, bossId, potionId, "Potion", 3)
        );

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        result
            .Distributions.Single()
            .Rewards.Select(reward => reward.Quantity)
            .Should()
            .BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task Handle_NoGroupReward_ReturnsAnEmptyDistribution()
    {
        // Arrange
        SetupRewards();

        // Act
        RunRewardDistribution result = await CreateHandler()
            .Handle(
                new GetRunRewardDistributionQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        result.DungeonRunId.Should().Be(runId);
        result.Distributions.Should().BeEmpty();
    }

    private GetRunRewardDistributionQueryHandler CreateHandler() => new(repository.Object);

    private void SetupRewards(params RewardEntity[] rewards) =>
        repository
            .Setup(source => source.GetGroupRewardsByRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rewards);

    private RewardEntity CreateReward(
        RewardDistribution distribution,
        Guid causeId,
        Guid itemId,
        string itemName,
        int quantity,
        int minutes = 0
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.NewGuid(),
            RecipientPlayerId = Guid.NewGuid(),
            RunId = runId,
            Distribution = distribution,
            CauseId = causeId,
            Status = RewardStatus.Applied,
            CreatedAt = Start.AddMinutes(minutes),
            RewardSource = new RewardSource { Id = Guid.NewGuid(), Name = "BOSS" },
            Items =
            [
                new RewardItem
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    ItemInstanceId = Guid.NewGuid(),
                    ItemNameSnapshot = itemName,
                    ItemRaritySnapshot = "RARE",
                    Quantity = quantity,
                },
            ],
        };
}
