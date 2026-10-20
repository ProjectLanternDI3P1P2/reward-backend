using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Rewards;

public sealed class AwardUniqueGroupRewardIntegrationTests(RewardGrantFixture fixture)
    : RewardGrantTestBase(fixture)
{
    [Fact]
    public async Task Award_SeveralEligiblePlayers_ExactlyOneOfThemReceivesTheReward()
    {
        // Arrange
        ActivityParticipant[] participants =
        [
            CreateParticipant(ParticipationStatus.Active),
            CreateParticipant(ParticipationStatus.Dead),
            CreateParticipant(ParticipationStatus.Disconnected),
        ];
        Guid itemId = await SeedAsync(participants);

        // Act
        AwardUniqueGroupRewardResult result = await SendAsync(CreateCommand(itemId, participants));

        // Assert
        result.AlreadyAwarded.Should().BeFalse();
        participants
            .Select(participant => participant.HeroId)
            .Should()
            .Contain(result.RecipientHeroId);
        (await GetHolderHeroIdsAsync(itemId)).Should().Equal(result.RecipientHeroId);
        (await CountRewardsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Award_PlayersWhoLeft_NeverReceiveTheReward()
    {
        // Arrange
        ActivityParticipant eligible = CreateParticipant(ParticipationStatus.Active);
        ActivityParticipant[] participants =
        [
            CreateParticipant(ParticipationStatus.Left),
            eligible,
            CreateParticipant(ParticipationStatus.Left),
        ];
        Guid itemId = await SeedAsync(participants);

        // Act
        AwardUniqueGroupRewardResult result = await SendAsync(CreateCommand(itemId, participants));

        // Assert
        result.RecipientHeroId.Should().Be(eligible.HeroId);
        (await GetHolderHeroIdsAsync(itemId)).Should().Equal(eligible.HeroId);
    }

    [Fact]
    public async Task Award_ReceivedAgain_NoOtherPlayerReceivesTheSameReward()
    {
        // Arrange
        ActivityParticipant[] participants =
        [
            CreateParticipant(ParticipationStatus.Active),
            CreateParticipant(ParticipationStatus.Active),
            CreateParticipant(ParticipationStatus.Active),
        ];
        Guid itemId = await SeedAsync(participants);
        AwardUniqueGroupRewardCommand command = CreateCommand(itemId, participants);
        AwardUniqueGroupRewardResult first = await SendAsync(command);

        // Act
        List<AwardUniqueGroupRewardResult> replays = [];
        for (int attempt = 0; attempt < 5; attempt++)
        {
            replays.Add(await SendAsync(command));
        }

        // Assert
        replays.Should().OnlyContain(replay => replay.AlreadyAwarded);
        replays.Should().OnlyContain(replay => replay.RecipientHeroId == first.RecipientHeroId);
        (await GetHolderHeroIdsAsync(itemId)).Should().Equal(first.RecipientHeroId);
        (await CountRewardsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Award_ConcurrentDeliveries_OnlyOnePlayerEverReceivesTheReward()
    {
        // Arrange
        ActivityParticipant[] participants = Enumerable
            .Range(0, 4)
            .Select(_ => CreateParticipant(ParticipationStatus.Active))
            .ToArray();
        Guid itemId = await SeedAsync(participants);
        AwardUniqueGroupRewardCommand command = CreateCommand(itemId, participants);

        // Act
        AwardUniqueGroupRewardResult[] results = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(_ => SendAsync(command))
        );

        // Assert
        results.Should().ContainSingle(result => !result.AlreadyAwarded);
        results.Select(result => result.RecipientHeroId).Distinct().Should().ContainSingle();
        (await GetHolderHeroIdsAsync(itemId)).Should().ContainSingle();
        (await CountRewardsAsync()).Should().Be(1);
    }

    private static ActivityParticipant CreateParticipant(ParticipationStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), status);

    private static AwardUniqueGroupRewardCommand CreateCommand(
        Guid itemId,
        IReadOnlyList<ActivityParticipant> participants
    ) => new(Guid.NewGuid(), RewardSourceType.Boss, Guid.NewGuid(), itemId, 1, participants);

    private async Task<AwardUniqueGroupRewardResult> SendAsync(
        AwardUniqueGroupRewardCommand command
    )
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send<AwardUniqueGroupRewardResult>(
            command,
            TestContext.Current.CancellationToken
        );
    }

    private async Task<Guid> SeedAsync(IReadOnlyList<ActivityParticipant> participants)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var rarity = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Legendary",
            Color = "#F59E0B",
            Rank = 5,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = "Blazing Aegis",
            Description = "A unique boss reward.",
            LevelRequired = 1,
            Stackable = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var source = new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "BOSS",
            Description = "Boss defeated in combat.",
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, item, source);
        dbContext.AddRange(
            participants.Select(participant => new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = participant.HeroId,
                ItemCapacity = 40,
                PotionCapacity = 20,
                CreatedAt = now,
                UpdatedAt = now,
            })
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return item.Id;
    }

    private async Task<IReadOnlyList<Guid>> GetHolderHeroIdsAsync(Guid itemId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Where(instance => instance.ItemId == itemId)
            .Select(instance => instance.Inventory.HeroId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> CountRewardsAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken);
    }
}
