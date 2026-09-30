using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.Chests.Controllers;

public sealed class ClaimChestContentsIntegrationTests(ChestControllerFixture fixture)
    : ChestControllerTestBase(fixture)
{
    [Fact]
    public async Task ClaimContents_WhenChestIsFilled_TransfersEveryRewardAndEmptiesTheChest()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedAsync(heroId, chestId, itemCapacity: 40);

        // Act
        HttpResponseMessage response = await ClaimAsync(heroId, chestId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ClaimChestContentsResponse? body =
            await response.Content.ReadFromJsonAsync<ClaimChestContentsResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should().NotBeNull();
        body!.ChestId.Should().Be(chestId);
        body.State.Should().Be("EMPTY");
        body.AlreadyEmpty.Should().BeFalse();
        body.Items.Should().HaveCount(2);

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        List<ItemInstance> itemInstances = await dbContext
            .ItemInstances.Include(itemInstance => itemInstance.Inventory)
            .ToListAsync(TestContext.Current.CancellationToken);
        itemInstances.Should().HaveCount(2);
        itemInstances.Should().OnlyContain(itemInstance => itemInstance.Inventory.HeroId == heroId);
        itemInstances
            .Should()
            .OnlyContain(itemInstance => itemInstance.Status == ItemInstanceStatus.Available);
        RewardEntity reward = await dbContext.Rewards.SingleAsync(
            TestContext.Current.CancellationToken
        );
        reward.Status.Should().Be("COMPLETED");
        List<RewardItem> rewardItems = await dbContext.RewardItems.ToListAsync(
            TestContext.Current.CancellationToken
        );
        rewardItems.Should().OnlyContain(rewardItem => rewardItem.ItemInstanceId != null);
    }

    [Fact]
    public async Task ClaimContents_WhenChestIsAlreadyEmpty_DoesNotTransferRewardsAgain()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedAsync(heroId, chestId, itemCapacity: 40);
        HttpResponseMessage firstResponse = await ClaimAsync(heroId, chestId);

        // Act
        HttpResponseMessage secondResponse = await ClaimAsync(heroId, chestId);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ClaimChestContentsResponse? body =
            await secondResponse.Content.ReadFromJsonAsync<ClaimChestContentsResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should().BeEquivalentTo(new ClaimChestContentsResponse(chestId, "EMPTY", [], true));
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(2);
    }

    [Fact]
    public async Task ClaimContents_WhenInventoryCapacityIsInsufficient_ReturnsConflictAndKeepsTheChestFilled()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedAsync(heroId, chestId, itemCapacity: 1);

        // Act
        HttpResponseMessage response = await ClaimAsync(heroId, chestId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(0);
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardEntity reward = await dbContext.Rewards.SingleAsync(
            TestContext.Current.CancellationToken
        );
        reward.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task ClaimContents_WhenChestDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await ClaimAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClaimContents_WhenChestIdIsEmpty_ReturnsUnprocessableEntity()
    {
        // Act
        HttpResponseMessage response = await ClaimAsync(Guid.NewGuid(), Guid.Empty);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private Task<HttpResponseMessage> ClaimAsync(Guid heroId, Guid chestId) =>
        Fixture.HttpClient.PostAsync(
            $"/api/v1/heroes/{heroId}/chests/{chestId}/claim",
            content: null,
            TestContext.Current.CancellationToken
        );

    // Seeds a chest holding two non-potion rewards: one sword and one armor.
    private async Task SeedAsync(Guid heroId, Guid chestId, int itemCapacity)
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
            Label = "Common",
            Color = "#FFFFFF",
            Rank = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };
        Item sword = CreateItem("Obsidian Blade", category, rarity, now);
        Item armor = CreateItem("Iron Plate", category, rarity, now);
        var rewardSource = new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "CHEST",
            Description = "Dungeon chest.",
        };
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            RewardSource = rewardSource,
            Type = "ITEM",
            Status = RewardStatus.Pending.ToCode(),
            RewardKey = Chest.CreateRewardKey(chestId),
            CreatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, inventory, sword, armor, rewardSource, reward);
        dbContext.RewardItems.AddRange(
            CreateRewardItem(reward, sword, now),
            CreateRewardItem(reward, armor, now)
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

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
            Description = "A reward.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static RewardItem CreateRewardItem(
        RewardEntity reward,
        Item item,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Reward = reward,
            Item = item,
            Quantity = 1,
            CreatedAt = now,
        };

    private async Task<int> CountItemInstancesAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ItemInstances.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record ClaimChestContentsResponse(
        Guid ChestId,
        string State,
        IReadOnlyList<ClaimedChestItemResponse> Items,
        bool AlreadyEmpty
    );

    private sealed record ClaimedChestItemResponse(Guid ItemInstanceId, Guid ItemId, int Quantity);
}
