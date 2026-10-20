using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.InventoryUseCase.ConsumeConsumable;
using Reward.Application.Features.RewardUseCase.GrantReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.Rewards;

public sealed class GrantRewardIntegrationTests(RewardGrantFixture fixture)
    : RewardGrantTestBase(fixture)
{
    [Fact]
    public async Task Grant_NewOperation_CreditsEachConfiguredItemInItsQuantityExactlyOnce()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid());
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue);

        // Act
        GrantRewardResult result = await SendAsync(command);

        // Assert
        result.AlreadyGranted.Should().BeFalse();
        (await GetInventoryStateAsync(catalogue.HeroId))
            .Should()
            .BeEquivalentTo([(catalogue.SwordId, 1), (catalogue.PotionId, 3)]);

        RewardEntity reward = await GetSingleRewardAsync();
        reward.Status.Should().Be(RewardStatus.Applied);
        reward.HeroId.Should().Be(catalogue.HeroId);
        reward.Items.Should().HaveCount(2);
        reward.Items.Should().OnlyContain(item => item.ItemInstanceId != null);
    }

    [Fact]
    public async Task Grant_SameOperationReceivedAgain_LeavesInventoryUnchanged()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid());
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue);
        GrantRewardResult first = await SendAsync(command);
        IReadOnlyList<(Guid, Guid, int)> inventoryBeforeReplay = await GetInventoryRowsAsync(
            catalogue.HeroId
        );

        // Act
        GrantRewardResult replay = await SendAsync(command);

        // Assert
        replay.AlreadyGranted.Should().BeTrue();
        replay.RewardId.Should().Be(first.RewardId);
        replay.Items.Should().BeEquivalentTo(first.Items);
        (await GetInventoryRowsAsync(catalogue.HeroId))
            .Should()
            .BeEquivalentTo(inventoryBeforeReplay);
        (await CountRewardsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Grant_ConcurrentDeliveriesOfTheSameOperation_CreditsOnlyOnce()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid());
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue);

        // Act
        GrantRewardResult[] results = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => SendAsync(command))
        );

        // Assert
        results.Should().ContainSingle(result => !result.AlreadyGranted);
        results.Select(result => result.RewardId).Distinct().Should().ContainSingle();
        (await GetInventoryStateAsync(catalogue.HeroId))
            .Should()
            .BeEquivalentTo([(catalogue.SwordId, 1), (catalogue.PotionId, 3)]);
        (await CountRewardsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Grant_SameCauseForAnotherHero_IsGrantedToEachHero()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid());
        Guid otherHeroId = await SeedInventoryAsync(Guid.NewGuid());
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue);

        // Act
        GrantRewardResult first = await SendAsync(command);
        GrantRewardResult second = await SendAsync(command with { HeroId = otherHeroId });

        // Assert
        first.AlreadyGranted.Should().BeFalse();
        second.AlreadyGranted.Should().BeFalse();
        (await GetInventoryStateAsync(otherHeroId))
            .Should()
            .BeEquivalentTo([(catalogue.SwordId, 1), (catalogue.PotionId, 3)]);
        (await CountRewardsAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Grant_WhenInventoryOverflows_PersistsNeitherRewardNorAnyItem()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid(), itemCapacity: 1);
        // The first weapon fits, the second overflows: the first one must be rolled back too.
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue) with
        {
            Items =
            [
                new GrantRewardItem(catalogue.SwordId, 1),
                new GrantRewardItem(catalogue.SwordId, 1),
            ],
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<InventoryCapacityExceededException>();
        (await CountRewardsAsync()).Should().Be(0);
        (await GetInventoryStateAsync(catalogue.HeroId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Grant_RewardedItemIsLaterUsedUp_KeepsTheRewardHistory()
    {
        // Arrange
        SeededCatalogue catalogue = await SeedAsync(Guid.NewGuid());
        GrantRewardCommand command = CreateCommand(catalogue.HeroId, catalogue) with
        {
            Items = [new GrantRewardItem(catalogue.PotionId, 1)],
        };
        GrantRewardResult granted = await SendAsync(command);
        Guid potionInstanceId = granted.Items.Single().ItemInstanceId!.Value;

        // Act
        await using (AsyncServiceScope scope = Fixture.Services.CreateAsyncScope())
        {
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send<ConsumeConsumableResult>(
                new ConsumeConsumableCommand(catalogue.HeroId, potionInstanceId, "consume-1"),
                TestContext.Current.CancellationToken
            );
        }

        // Assert
        (await GetInventoryStateAsync(catalogue.HeroId))
            .Should()
            .BeEmpty();
        RewardEntity reward = await GetSingleRewardAsync();
        RewardItem rewardItem = reward.Items.Single();
        rewardItem.ItemId.Should().Be(catalogue.PotionId);
        rewardItem.Quantity.Should().Be(1);
        rewardItem.ItemInstanceId.Should().BeNull();
    }

    private static GrantRewardCommand CreateCommand(Guid heroId, SeededCatalogue catalogue) =>
        new(
            heroId,
            catalogue.RunId,
            RewardSourceType.Monster,
            catalogue.CauseId,
            [new GrantRewardItem(catalogue.SwordId, 1), new GrantRewardItem(catalogue.PotionId, 3)]
        );

    private async Task<GrantRewardResult> SendAsync(GrantRewardCommand command)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send<GrantRewardResult>(command, TestContext.Current.CancellationToken);
    }

    private async Task<SeededCatalogue> SeedAsync(Guid heroId, int itemCapacity = 40)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weapons = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potions = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var rarity = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Common",
            Color = "#FFFFFF",
            Rank = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        Item sword = CreateItem("Iron Sword", weapons, rarity, now);
        Item potion = CreateItem("Health Potion", potions, rarity, now);
        var source = new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "MONSTER",
            Description = "Standard monster defeated in combat.",
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(weapons, potions, rarity, sword, potion, source);
        dbContext.Add(CreateInventory(heroId, itemCapacity, now));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededCatalogue(heroId, Guid.NewGuid(), Guid.NewGuid(), sword.Id, potion.Id);
    }

    private async Task<Guid> SeedInventoryAsync(Guid heroId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.Add(CreateInventory(heroId, itemCapacity: 40, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return heroId;
    }

    private static InventoryEntity CreateInventory(
        Guid heroId,
        int itemCapacity,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static Item CreateItem(
        string name,
        Category category,
        Rarity rarity,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = name,
            Description = name,
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private async Task<IReadOnlyList<(Guid ItemId, int Quantity)>> GetInventoryStateAsync(
        Guid heroId
    ) => (await GetInventoryRowsAsync(heroId)).Select(row => (row.ItemId, row.Quantity)).ToList();

    private async Task<
        IReadOnlyList<(Guid ItemInstanceId, Guid ItemId, int Quantity)>
    > GetInventoryRowsAsync(Guid heroId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        var rows = await dbContext
            .ItemInstances.Where(instance => instance.Inventory.HeroId == heroId)
            .Select(instance => new
            {
                instance.Id,
                instance.ItemId,
                instance.Quantity,
            })
            .ToListAsync(TestContext.Current.CancellationToken);
        return rows.Select(row => (row.Id, row.ItemId, row.Quantity)).ToList();
    }

    private async Task<RewardEntity> GetSingleRewardAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .Rewards.Include(reward => reward.Items)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> CountRewardsAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record SeededCatalogue(
        Guid HeroId,
        Guid RunId,
        Guid CauseId,
        Guid SwordId,
        Guid PotionId
    );
}
