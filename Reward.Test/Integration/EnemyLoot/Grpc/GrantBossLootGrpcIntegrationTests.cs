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

public sealed class GrantBossLootGrpcIntegrationTests(EnemyLootGrpcFixture fixture)
    : EnemyLootGrpcTestBase(fixture)
{
    [Fact]
    public async Task GrantEnemyLoot_WonBossCombat_GrantsAtLeastOneRareEquipmentFromTheBossLootTable()
    {
        // Arrange a boss table that almost always draws the common potion first.
        SeededBoss seeded = await SeedAsync();
        Guid enemyId = Guid.NewGuid();

        // Act
        EnemyLootReply reply = await GrantAsync(CreateBossRequest(enemyId, seeded));

        // Assert the guaranteed equipment is part of the reply, and nothing comes from elsewhere.
        HeroEnemyLoot heroLoot = reply.HeroLoots.Should().ContainSingle().Subject;
        heroLoot.Items.Select(item => Guid.Parse(item.ItemId)).Should().Contain(seeded.BladeId);
        heroLoot
            .Items.Should()
            .OnlyContain(item =>
                Guid.Parse(item.ItemId) == seeded.PotionId
                || Guid.Parse(item.ItemId) == seeded.BladeId
            );

        // Assert the equipment is in the hero inventory and the boss reward is persisted once.
        (await GetInventoryItemIdsAsync(seeded.Hero.HeroId))
            .Should()
            .Contain(seeded.BladeId);
        RewardEntity reward = (await GetRewardsAsync()).Should().ContainSingle().Subject;
        reward
            .RewardKey.Should()
            .Be(RewardEntity.CreateKey(RewardSourceType.Boss, enemyId, seeded.Hero.HeroId));
        reward.Status.Should().Be(RewardStatus.Applied);
    }

    [Fact]
    public async Task GrantEnemyLoot_LostBossCombat_GeneratesNoBossReward()
    {
        // Arrange
        SeededBoss seeded = await SeedAsync();
        GrantEnemyLootRequest request = CreateBossRequest(Guid.NewGuid(), seeded);
        request.CombatResult = CombatResult.Lost;

        // Act
        EnemyLootReply reply = await GrantAsync(request);

        // Assert
        reply.HeroLoots.Should().BeEmpty();
        (await GetRewardsAsync()).Should().BeEmpty();
        (await GetInventoryItemIdsAsync(seeded.Hero.HeroId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GrantEnemyLoot_BossLootTableWithoutRareEquipment_ReturnsFailedPreconditionAndPersistsNothing()
    {
        // Arrange a misconfigured boss table that cannot honour the guarantee.
        SeededBoss seeded = await SeedAsync(includeRareEquipment: false);

        // Act
        Func<Task> action = () => GrantAsync(CreateBossRequest(Guid.NewGuid(), seeded));

        // Assert
        await ShouldFailWithAsync(action, StatusCode.FailedPrecondition);
        (await GetRewardsAsync()).Should().BeEmpty();
        (await GetInventoryItemIdsAsync(seeded.Hero.HeroId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GrantEnemyLoot_BossReceivedAgain_ReturnsTheSameLootWithoutDrawingAgain()
    {
        // Arrange
        SeededBoss seeded = await SeedAsync();
        GrantEnemyLootRequest request = CreateBossRequest(Guid.NewGuid(), seeded);
        EnemyLootReply first = await GrantAsync(request);
        IReadOnlyList<Guid> inventoryBeforeReplay = await GetInventoryItemIdsAsync(
            seeded.Hero.HeroId
        );

        // Act
        EnemyLootReply replay = await GrantAsync(request);

        // Assert
        HeroEnemyLoot replayedLoot = replay.HeroLoots.Should().ContainSingle().Subject;
        replayedLoot.AlreadyGranted.Should().BeTrue();
        replayedLoot.RewardId.Should().Be(first.HeroLoots[0].RewardId);
        (await GetRewardsAsync()).Should().ContainSingle();
        (await GetInventoryItemIdsAsync(seeded.Hero.HeroId))
            .Should()
            .BeEquivalentTo(inventoryBeforeReplay);
    }

    private static GrantEnemyLootRequest CreateBossRequest(Guid enemyId, SeededBoss seeded)
    {
        GrantEnemyLootRequest request = CreateRequest(
            Guid.NewGuid(),
            enemyId,
            seeded.LootTableId,
            CombatResult.Won,
            seeded.Hero
        );
        request.EnemyKind = EnemyKind.Boss;
        return request;
    }

    // Seeds a boss table that draws one item: a very likely common potion and, when requested,
    // a very unlikely rare weapon that only the guarantee makes certain.
    private async Task<SeededBoss> SeedAsync(bool includeRareEquipment = true)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weapon = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potionCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
            CreatedAt = now,
            UpdatedAt = now,
        };
        Rarity common = CreateRarity("Common", rank: 1, now);
        Rarity rare = CreateRarity("Rare", rank: 3, now);
        Item potion = CreateItem("Health Potion", potionCategory, common, now);
        Item blade = CreateItem("Obsidian Blade", weapon, rare, now);

        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Name = "Boss - Normal",
            Difficulty = "NORMAL",
            SourceType = "BOSS",
            DrawCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        table.RarityRules.Add(CreateRule(table, common, weight: 1000, potion));
        if (includeRareEquipment)
        {
            table.RarityRules.Add(CreateRule(table, rare, weight: 1, blade));
        }

        var hero = new SeededHero(Guid.NewGuid(), Guid.NewGuid(), ParticipantStatus.Active);
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = hero.HeroId,
            ItemCapacity = 40,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(weapon, potionCategory, common, rare, potion, blade, table, inventory);
        dbContext.RewardSources.AddRange(
            CreateSource("MONSTER"),
            CreateSource("BOSS"),
            CreateSource("CHEST")
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeededBoss(table.Id, potion.Id, blade.Id, hero);
    }

    private static Rarity CreateRarity(string label, int rank, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Label = label,
            Color = "#FFFFFF",
            Rank = rank,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static LootRarityRule CreateRule(LootTable table, Rarity rarity, int weight, Item item)
    {
        var rule = new LootRarityRule
        {
            Id = Guid.NewGuid(),
            LootTable = table,
            Rarity = rarity,
            Weight = weight,
        };
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
        return rule;
    }

    private sealed record SeededBoss(
        Guid LootTableId,
        Guid PotionId,
        Guid BladeId,
        SeededHero Hero
    );
}
