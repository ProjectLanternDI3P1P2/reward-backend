using FluentAssertions;
using Moq;
using Reward.Application.Features.RewardUseCase.GrantEnemyLoot;
using Reward.Application.Features.RewardUseCase.GrantReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;
using Serilog;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.RewardUseCase;

public sealed class GrantEnemyLootCommandHandlerTests
{
    private readonly Guid runId = Guid.NewGuid();
    private readonly Guid enemyId = Guid.NewGuid();
    private readonly Item sword = CreateItem("Iron Sword", "WEAPON");
    private readonly Item shield = CreateItem("Oak Shield", "SHIELD");
    private readonly LootTable table;
    private readonly Mock<ILootTableRepository> lootTableRepository = new();
    private readonly Mock<IRewardRepository> rewardRepository = new();
    private readonly Mock<IInventoryRepository> inventoryRepository = new();

    public GrantEnemyLootCommandHandlerTests()
    {
        table = CreateTable("MONSTER", sword, shield);
        lootTableRepository
            .Setup(repository => repository.GetByIdAsync(table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        rewardRepository
            .Setup(repository =>
                repository.GetSourceByNameAsync("MONSTER", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new RewardSource { Id = Guid.NewGuid(), Name = "MONSTER" });
    }

    [Fact]
    public async Task Handle_WonCombat_GrantsLootFromTheEnemyLootTable()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        Inventory inventory = SetupInventory(participant.HeroId);
        RewardEntity? addedReward = null;
        rewardRepository
            .Setup(repository => repository.Add(It.IsAny<RewardEntity>()))
            .Callback<RewardEntity>(reward => addedReward = reward);

        // Act
        GrantEnemyLootResult result = await CreateHandler()
            .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        result.EnemyId.Should().Be(enemyId);
        GrantedHeroLoot heroLoot = result.HeroLoots.Should().ContainSingle().Subject;
        heroLoot.HeroId.Should().Be(participant.HeroId);
        heroLoot.AlreadyGranted.Should().BeFalse();
        heroLoot.Items.Should().NotBeEmpty();
        heroLoot
            .Items.Should()
            .OnlyContain(item => item.ItemId == sword.Id || item.ItemId == shield.Id);
        inventory
            .ItemInstances.Select(instance => instance.ItemId)
            .Should()
            .BeEquivalentTo(heroLoot.Items.Select(item => item.ItemId));
        addedReward.Should().NotBeNull();
        addedReward!.Status.Should().Be(RewardStatus.Applied);
        addedReward.HeroId.Should().Be(participant.HeroId);
        addedReward.RunId.Should().Be(runId);
        addedReward
            .RewardKey.Should()
            .Be(RewardEntity.CreateKey(RewardSourceType.Monster, enemyId, participant.HeroId));
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Exactly(heroLoot.Items.Count)
        );
    }

    [Fact]
    public async Task Handle_LostCombat_GeneratesNoLootAndDrawsNothing()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        GrantEnemyLootCommandHandler handler = CreateHandler(new ThrowingRandom());

        // Act
        GrantEnemyLootResult result = await handler.Handle(
            CreateCommand(participant) with
            {
                Outcome = CombatOutcome.Lost,
            },
            TestContext.Current.CancellationToken
        );

        // Assert
        result.EnemyId.Should().Be(enemyId);
        result.HeroLoots.Should().BeEmpty();
        lootTableRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_UnknownLootTable_ThrowsNotFound()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        GrantEnemyLootCommand command = CreateCommand(participant) with
        {
            LootTableId = Guid.NewGuid(),
        };
        lootTableRepository
            .Setup(repository =>
                repository.GetByIdAsync(command.LootTableId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((LootTable?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<LootTableNotFoundException>();
    }

    [Fact]
    public async Task Handle_LootTableOfAnotherSource_ThrowsInvalidLootTable()
    {
        // Arrange
        LootTable chestTable = CreateTable("CHEST", sword);
        lootTableRepository
            .Setup(repository =>
                repository.GetByIdAsync(chestTable.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(chestTable);
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        SetupInventory(participant.HeroId);

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(
                    CreateCommand(participant) with
                    {
                        LootTableId = chestTable.Id,
                    },
                    TestContext.Current.CancellationToken
                );

        // Assert
        await action
            .Should()
            .ThrowAsync<InvalidLootTableException>()
            .WithMessage("*cannot reward a MONSTER enemy*");
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_RewardSourceNotConfigured_Throws()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        rewardRepository
            .Setup(repository =>
                repository.GetSourceByNameAsync("MONSTER", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((RewardSource?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        await action
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*'MONSTER' is not configured*");
    }

    [Fact]
    public async Task Handle_EveryEligibleParticipant_ReceivesTheirOwnReward()
    {
        // Arrange
        ActivityParticipant active = CreateParticipant(ParticipationStatus.Active);
        ActivityParticipant dead = CreateParticipant(ParticipationStatus.Dead);
        Inventory activeInventory = SetupInventory(active.HeroId);
        Inventory deadInventory = SetupInventory(dead.HeroId);

        // Act
        GrantEnemyLootResult result = await CreateHandler()
            .Handle(CreateCommand(active, dead), TestContext.Current.CancellationToken);

        // Assert
        result
            .HeroLoots.Select(heroLoot => heroLoot.HeroId)
            .Should()
            .BeEquivalentTo([active.HeroId, dead.HeroId]);
        result.HeroLoots.Select(heroLoot => heroLoot.RewardId).Distinct().Should().HaveCount(2);
        activeInventory.ItemInstances.Should().NotBeEmpty();
        deadInventory.ItemInstances.Should().NotBeEmpty();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_ParticipantWhoLeft_ReceivesNothing()
    {
        // Arrange
        ActivityParticipant active = CreateParticipant(ParticipationStatus.Active);
        ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);
        SetupInventory(active.HeroId);

        // Act
        GrantEnemyLootResult result = await CreateHandler()
            .Handle(CreateCommand(active, left), TestContext.Current.CancellationToken);

        // Assert
        result.HeroLoots.Should().ContainSingle().Which.HeroId.Should().Be(active.HeroId);
        inventoryRepository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(left.HeroId, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_NoEligibleParticipant_GrantsNothing()
    {
        // Arrange
        ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);

        // Act
        GrantEnemyLootResult result = await CreateHandler()
            .Handle(CreateCommand(left), TestContext.Current.CancellationToken);

        // Assert
        result.HeroLoots.Should().BeEmpty();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReplayedOperation_ReturnsThePersistedLootWithoutDrawingAgain()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        RewardEntity existing = CreateExistingReward(participant.HeroId, RewardStatus.Applied);
        SetupExistingReward(participant.HeroId, existing);
        GrantEnemyLootCommandHandler handler = CreateHandler(new ThrowingRandom());

        // Act
        GrantEnemyLootResult result = await handler.Handle(
            CreateCommand(participant),
            TestContext.Current.CancellationToken
        );

        // Assert
        GrantedHeroLoot heroLoot = result.HeroLoots.Should().ContainSingle().Subject;
        heroLoot.AlreadyGranted.Should().BeTrue();
        heroLoot.RewardId.Should().Be(existing.Id);
        heroLoot
            .Items.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new GrantedRewardItem(sword.Id, null, 2));
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
        inventoryRepository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_OperationGrantedWhileWaitingForTheInventoryLock_ReturnsThePersistedLoot()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        SetupInventory(participant.HeroId);
        RewardEntity existing = CreateExistingReward(participant.HeroId, RewardStatus.Applied);
        string rewardKey = RewardEntity.CreateKey(
            RewardSourceType.Monster,
            enemyId,
            participant.HeroId
        );
        rewardRepository
            .SetupSequence(repository =>
                repository.GetByKeyAsync(rewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((RewardEntity?)null)
            .ReturnsAsync(existing);

        // Act
        GrantEnemyLootResult result = await CreateHandler()
            .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        result.HeroLoots.Should().ContainSingle().Which.AlreadyGranted.Should().BeTrue();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ExistingRewardNotApplied_Throws()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        SetupExistingReward(
            participant.HeroId,
            CreateExistingReward(participant.HeroId, RewardStatus.Pending)
        );

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not applied*");
    }

    [Fact]
    public async Task Handle_ExistingRewardOfAnotherRun_Throws()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        RewardEntity existing = CreateExistingReward(participant.HeroId, RewardStatus.Applied);
        existing.RunId = Guid.NewGuid();
        SetupExistingReward(participant.HeroId, existing);

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*another run*");
    }

    [Fact]
    public async Task Handle_UnknownInventory_ThrowsNotFound()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(
                    participant.HeroId,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Inventory?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Inventory *");
    }

    [Fact]
    public async Task Handle_InventoryFull_ThrowsAndAddsNoItem()
    {
        // Arrange
        ActivityParticipant participant = CreateParticipant(ParticipationStatus.Active);
        SetupInventory(participant.HeroId, itemCapacity: 0);

        // Act
        Func<Task> action = () =>
            CreateHandler()
                .Handle(CreateCommand(participant), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<InventoryCapacityExceededException>();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        // Act
        var result = new GrantEnemyLootCommandValidator().Validate(
            CreateCommand(CreateParticipant(ParticipationStatus.Active))
        );

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCommand_ReportsEveryProblem()
    {
        // Arrange
        ActivityParticipant duplicated = CreateParticipant(ParticipationStatus.Active);
        var command = new GrantEnemyLootCommand(
            Guid.Empty,
            Guid.Empty,
            RewardSourceType.Chest,
            Guid.Empty,
            (CombatOutcome)99,
            [duplicated, duplicated]
        );

        // Act
        var result = new GrantEnemyLootCommandValidator().Validate(command);

        // Assert
        result
            .Errors.Select(error => error.PropertyName)
            .Should()
            .Contain([
                nameof(GrantEnemyLootCommand.RunId),
                nameof(GrantEnemyLootCommand.EnemyId),
                nameof(GrantEnemyLootCommand.EnemyType),
                nameof(GrantEnemyLootCommand.LootTableId),
                nameof(GrantEnemyLootCommand.Outcome),
                nameof(GrantEnemyLootCommand.Participants),
            ]);
        result
            .Errors.Select(error => error.ErrorMessage)
            .Should()
            .Contain([
                "A player cannot be registered more than once.",
                "A hero cannot be registered more than once.",
            ]);
    }

    [Fact]
    public void Validate_NoParticipantOrInvalidParticipant_ReportsAnError()
    {
        // Arrange
        var validator = new GrantEnemyLootCommandValidator();
        var invalidParticipant = new ActivityParticipant(
            Guid.Empty,
            Guid.Empty,
            (ParticipationStatus)99
        );

        // Act
        var withoutParticipants = validator.Validate(CreateCommand());
        var withInvalidParticipant = validator.Validate(CreateCommand(invalidParticipant));

        // Assert
        withoutParticipants.IsValid.Should().BeFalse();
        withInvalidParticipant.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    private GrantEnemyLootCommandHandler CreateHandler(IRandomNumberGenerator? random = null) =>
        new(
            lootTableRepository.Object,
            rewardRepository.Object,
            inventoryRepository.Object,
            new LootSelectionService(random ?? new FirstRandom()),
            new FixedClock(),
            new LoggerConfiguration().CreateLogger()
        );

    private GrantEnemyLootCommand CreateCommand(params ActivityParticipant[] participants) =>
        new(runId, enemyId, RewardSourceType.Monster, table.Id, CombatOutcome.Won, participants);

    private Inventory SetupInventory(Guid heroId, int itemCapacity = 40)
    {
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
        };
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        return inventory;
    }

    private void SetupExistingReward(Guid heroId, RewardEntity existing) =>
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(
                    RewardEntity.CreateKey(RewardSourceType.Monster, enemyId, heroId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existing);

    private RewardEntity CreateExistingReward(Guid heroId, RewardStatus status)
    {
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            RunId = runId,
            Status = status,
            RewardKey = RewardEntity.CreateKey(RewardSourceType.Monster, enemyId, heroId),
        };
        reward.Items.Add(new RewardItem { ItemId = sword.Id, Quantity = 2 });
        return reward;
    }

    private static ActivityParticipant CreateParticipant(ParticipationStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), status);

    private static Item CreateItem(string name, string category)
    {
        var rarity = new Rarity { Id = CommonRarityId, Label = "Common" };
        return new Item
        {
            Id = Guid.NewGuid(),
            Name = name,
            RarityId = rarity.Id,
            Rarity = rarity,
            Category = new Category { Label = category },
        };
    }

    private static LootTable CreateTable(string sourceType, params Item[] items)
    {
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Difficulty = "NORMAL",
            SourceType = sourceType,
            DrawCount = 1,
        };
        table.RarityRules.Add(
            new LootRarityRule
            {
                RarityId = CommonRarityId,
                Weight = 1,
                Entries = items
                    .Select(item => new LootTableEntry
                    {
                        ItemId = item.Id,
                        Item = item,
                        Weight = 1,
                        MinQuantity = 1,
                        MaxQuantity = 1,
                    })
                    .ToList(),
            }
        );
        return table;
    }

    private static readonly Guid CommonRarityId = Guid.NewGuid();

    // Always picks the first interval, which makes every draw reproducible.
    private sealed class FirstRandom : IRandomNumberGenerator
    {
        public long NextInt64(long exclusiveMaximum) => 0;
    }

    private sealed class ThrowingRandom : IRandomNumberGenerator
    {
        public long NextInt64(long exclusiveMaximum) =>
            throw new InvalidOperationException("No loot must be drawn.");
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    }
}
