using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Contracts.V1;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.EnemyLoot.Grpc;

public sealed class GrantEnemyLootGrpcIntegrationTests(EnemyLootGrpcFixture fixture)
    : EnemyLootGrpcTestBase(fixture)
{
    private const int DrawCount = 2;

    [Fact]
    public async Task GrantEnemyLoot_WonCombat_CreditsItemsOfTheEnemyLootTableAndPersistsTheReward()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);
        Guid runId = Guid.NewGuid();
        Guid enemyId = Guid.NewGuid();

        // Act
        EnemyLootReply reply = await GrantAsync(
            CreateRequest(runId, enemyId, seeded.LootTableId, CombatResult.Won, seeded.Heroes[0])
        );

        // Assert the reply only exposes items drawn from the enemy's table.
        reply.EnemyId.Should().Be(enemyId.ToString());
        HeroEnemyLoot heroLoot = reply.HeroLoots.Should().ContainSingle().Subject;
        heroLoot.HeroId.Should().Be(seeded.Heroes[0].HeroId.ToString());
        heroLoot.AlreadyGranted.Should().BeFalse();
        heroLoot
            .Items.Should()
            .OnlyContain(item => seeded.ItemIds.Contains(Guid.Parse(item.ItemId)));
        heroLoot.Items.Sum(item => item.Quantity).Should().Be(DrawCount);

        // Assert the items are in the hero inventory and the reward is persisted once.
        (await GetInventoryItemIdsAsync(seeded.Heroes[0].HeroId))
            .Should()
            .BeSubsetOf(seeded.ItemIds)
            .And.NotBeEmpty();
        RewardEntity reward = (await GetRewardsAsync()).Should().ContainSingle().Subject;
        reward.HeroId.Should().Be(seeded.Heroes[0].HeroId);
        reward.RunId.Should().Be(runId);
        reward.Status.Should().Be(RewardStatus.Applied);
        reward
            .RewardKey.Should()
            .Be(RewardEntity.CreateKey(RewardSourceType.Monster, enemyId, seeded.Heroes[0].HeroId));
        reward.Items.Should().OnlyContain(item => item.ItemInstanceId != null);
    }

    [Fact]
    public async Task GrantEnemyLoot_LostCombat_GeneratesNoReward()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);

        // Act
        EnemyLootReply reply = await GrantAsync(
            CreateRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                seeded.LootTableId,
                CombatResult.Lost,
                seeded.Heroes[0]
            )
        );

        // Assert
        reply.HeroLoots.Should().BeEmpty();
        (await GetRewardsAsync()).Should().BeEmpty();
        (await GetInventoryItemIdsAsync(seeded.Heroes[0].HeroId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GrantEnemyLoot_SameEnemyReceivedAgain_ReturnsTheSameLootWithoutDuplicatingIt()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seeded.LootTableId,
            CombatResult.Won,
            seeded.Heroes[0]
        );
        EnemyLootReply first = await GrantAsync(request);
        IReadOnlyList<Guid> inventoryBeforeReplay = await GetInventoryItemIdsAsync(
            seeded.Heroes[0].HeroId
        );

        // Act
        EnemyLootReply replay = await GrantAsync(request);

        // Assert
        HeroEnemyLoot replayedLoot = replay.HeroLoots.Should().ContainSingle().Subject;
        replayedLoot.AlreadyGranted.Should().BeTrue();
        replayedLoot.RewardId.Should().Be(first.HeroLoots[0].RewardId);
        replayedLoot.Items.Should().BeEquivalentTo(first.HeroLoots[0].Items);
        (await GetRewardsAsync()).Should().ContainSingle();
        (await GetInventoryItemIdsAsync(seeded.Heroes[0].HeroId))
            .Should()
            .BeEquivalentTo(inventoryBeforeReplay);
    }

    [Fact]
    public async Task GrantEnemyLoot_ParticipantWhoLeft_ReceivesNothing()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 2);
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seeded.LootTableId,
            CombatResult.Won,
            seeded.Heroes[0],
            seeded.Heroes[1] with
            {
                Status = ParticipantStatus.Left,
            }
        );

        // Act
        EnemyLootReply reply = await GrantAsync(request);

        // Assert
        reply
            .HeroLoots.Should()
            .ContainSingle()
            .Which.HeroId.Should()
            .Be(seeded.Heroes[0].HeroId.ToString());
        (await GetInventoryItemIdsAsync(seeded.Heroes[1].HeroId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GrantEnemyLoot_UnknownLootTable_ReturnsNotFound()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);

        // Act
        Func<Task> action = () =>
            GrantAsync(
                CreateRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    CombatResult.Won,
                    seeded.Heroes[0]
                )
            );

        // Assert
        await ShouldFailWithAsync(action, StatusCode.NotFound);
    }

    [Fact]
    public async Task GrantEnemyLoot_LootTableOfAnotherSource_ReturnsFailedPrecondition()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1, lootTableSource: "CHEST");

        // Act
        Func<Task> action = () =>
            GrantAsync(
                CreateRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    seeded.LootTableId,
                    CombatResult.Won,
                    seeded.Heroes[0]
                )
            );

        // Assert
        await ShouldFailWithAsync(action, StatusCode.FailedPrecondition);
        (await GetRewardsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GrantEnemyLoot_InventoryFull_ReturnsFailedPreconditionAndPersistsNothing()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1, fillInventory: true);
        int itemCountBefore = (await GetInventoryItemIdsAsync(seeded.Heroes[0].HeroId)).Count;

        // Act
        Func<Task> action = () =>
            GrantAsync(
                CreateRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    seeded.LootTableId,
                    CombatResult.Won,
                    seeded.Heroes[0]
                )
            );

        // Assert
        await ShouldFailWithAsync(action, StatusCode.FailedPrecondition);
        (await GetRewardsAsync()).Should().BeEmpty();
        (await GetInventoryItemIdsAsync(seeded.Heroes[0].HeroId))
            .Should()
            .HaveCount(itemCountBefore);
    }

    [Fact]
    public async Task GrantEnemyLoot_MalformedIdentifier_ReturnsInvalidArgument()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seeded.LootTableId,
            CombatResult.Won,
            seeded.Heroes[0]
        );
        request.RunId = "not-a-guid";

        // Act
        Func<Task> action = () => GrantAsync(request);

        // Assert
        await ShouldFailWithAsync(action, StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData("kind")]
    [InlineData("result")]
    [InlineData("status")]
    [InlineData("participant-hero")]
    public async Task GrantEnemyLoot_UnspecifiedOrMalformedField_ReturnsInvalidArgument(
        string field
    )
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seeded.LootTableId,
            CombatResult.Won,
            seeded.Heroes[0]
        );
        switch (field)
        {
            case "kind":
                request.EnemyKind = EnemyKind.Unspecified;
                break;
            case "result":
                request.CombatResult = CombatResult.Unspecified;
                break;
            case "status":
                request.Participants[0].Status = ParticipantStatus.Unspecified;
                break;
            default:
                request.Participants[0].HeroId = "not-a-guid";
                break;
        }

        // Act
        Func<Task> action = () => GrantAsync(request);

        // Assert
        await ShouldFailWithAsync(action, StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GrantEnemyLoot_RequestWithoutParticipants_ReturnsInvalidArgument()
    {
        // Arrange
        SeededEnemy seeded = await SeedAsync(heroCount: 1);
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seeded.LootTableId,
            CombatResult.Won
        );

        // Act
        Func<Task> action = () => GrantAsync(request);

        // Assert
        await ShouldFailWithAsync(action, StatusCode.InvalidArgument);
    }

    private Task<EnemyLootReply> GrantAsync(GrantEnemyLootRequest request) =>
        Fixture
            .CreateClient()
            .GrantEnemyLootAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ResponseAsync;

    private static async Task ShouldFailWithAsync(Func<Task> action, StatusCode statusCode) =>
        await action
            .Should()
            .ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == statusCode);

    private static GrantEnemyLootRequest CreateRequest(
        Guid runId,
        Guid enemyId,
        Guid lootTableId,
        CombatResult result,
        params SeededHero[] participants
    )
    {
        var request = new GrantEnemyLootRequest
        {
            RunId = runId.ToString(),
            EnemyId = enemyId.ToString(),
            EnemyKind = EnemyKind.Monster,
            LootTableId = lootTableId.ToString(),
            CombatResult = result,
        };
        request.Participants.AddRange(
            participants.Select(participant => new EnemyLootParticipant
            {
                PlayerId = participant.PlayerId.ToString(),
                HeroId = participant.HeroId.ToString(),
                Status = participant.Status,
            })
        );
        return request;
    }

    // Seeds the catalogue, reward sources, a monster loot table drawing two items from two
    // catalogue items, and one inventory per hero.
    private async Task<SeededEnemy> SeedAsync(
        int heroCount,
        string lootTableSource = "MONSTER",
        bool fillInventory = false
    )
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
        Item sword = CreateItem("Iron Sword", category, rarity, now);
        Item shield = CreateItem("Oak Shield", category, rarity, now);
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Name = "Standard monster - Normal",
            Difficulty = "NORMAL",
            SourceType = lootTableSource,
            DrawCount = DrawCount,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var rule = new LootRarityRule
        {
            Id = Guid.NewGuid(),
            LootTable = table,
            Rarity = rarity,
            Weight = 1,
        };
        foreach (Item item in new[] { sword, shield })
        {
            rule.Entries.Add(
                new LootTableEntry
                {
                    Id = Guid.NewGuid(),
                    LootRarityRule = rule,
                    Item = item,
                    Weight = 1,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                }
            );
        }
        table.RarityRules.Add(rule);

        List<SeededHero> heroes = Enumerable
            .Range(0, heroCount)
            .Select(_ => new SeededHero(Guid.NewGuid(), Guid.NewGuid(), ParticipantStatus.Active))
            .ToList();
        List<InventoryEntity> inventories = heroes
            .Select(hero => new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = hero.HeroId,
                ItemCapacity = fillInventory ? 1 : 40,
                PotionCapacity = 20,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, sword, shield, table);
        dbContext.RewardSources.AddRange(
            CreateSource("MONSTER"),
            CreateSource("BOSS"),
            CreateSource("CHEST")
        );
        dbContext.Inventories.AddRange(inventories);
        if (fillInventory)
        {
            dbContext.ItemInstances.AddRange(
                inventories.Select(inventory => new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    Item = sword,
                    Inventory = inventory,
                    Status = ItemInstanceStatus.Available,
                    Quantity = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            );
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeededEnemy(table.Id, [sword.Id, shield.Id], heroes);
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
            Description = "Enemy loot.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static RewardSource CreateSource(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name,
        };

    private async Task<IReadOnlyList<Guid>> GetInventoryItemIdsAsync(Guid heroId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Where(itemInstance => itemInstance.Inventory.HeroId == heroId)
            .OrderBy(itemInstance => itemInstance.Id)
            .Select(itemInstance => itemInstance.ItemId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IReadOnlyList<RewardEntity>> GetRewardsAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .Rewards.Include(reward => reward.Items)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed record SeededHero(Guid PlayerId, Guid HeroId, ParticipantStatus Status);

    private sealed record SeededEnemy(
        Guid LootTableId,
        IReadOnlyList<Guid> ItemIds,
        IReadOnlyList<SeededHero> Heroes
    );
}
