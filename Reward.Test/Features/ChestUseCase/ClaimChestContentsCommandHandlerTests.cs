using FluentAssertions;
using Moq;
using Reward.Application.Features.ChestUseCase.ClaimChestContents;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.ChestUseCase;

public sealed class ClaimChestContentsCommandHandlerTests
{
    [Fact]
    public async Task Handle_FilledChestAndEnoughCapacity_TransfersEveryRewardToTheInventory()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        RewardEntity reward = CreateReward(chestId, RewardStatus.Pending);
        RewardItem sword = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        RewardItem potion = CreateRewardItem(reward, "POTION", stackable: true, quantity: 3);
        Inventory inventory = CreateInventory(heroId);
        Mock<IRewardRepository> rewardRepository = CreateRewardRepository(reward, sword, potion);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        var handler = new ClaimChestContentsCommandHandler(
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock()
        );

        // Act
        ClaimChestContentsResult result = await handler.Handle(
            new ClaimChestContentsCommand(heroId, chestId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.ChestId.Should().Be(chestId);
        result.State.Should().Be("EMPTY");
        result.AlreadyEmpty.Should().BeFalse();
        result
            .Items.Select(item => new { item.ItemId, item.Quantity })
            .Should()
            .BeEquivalentTo([
                new { sword.ItemId, Quantity = 1 },
                new { potion.ItemId, Quantity = 3 },
            ]);
        inventory
            .ItemInstances.Select(itemInstance => itemInstance.Id)
            .Should()
            .BeEquivalentTo(result.Items.Select(item => item.ItemInstanceId));
        reward.Status.Should().Be("COMPLETED");
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_EmptyChest_ReturnsEmptyStateWithoutTransferringAnything()
    {
        // Arrange
        Guid chestId = Guid.NewGuid();
        RewardEntity reward = CreateReward(chestId, RewardStatus.Completed);
        RewardItem sword = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        Mock<IRewardRepository> rewardRepository = CreateRewardRepository(reward, sword);
        var inventoryRepository = new Mock<IInventoryRepository>();
        var handler = new ClaimChestContentsCommandHandler(
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock()
        );

        // Act
        ClaimChestContentsResult result = await handler.Handle(
            new ClaimChestContentsCommand(Guid.NewGuid(), chestId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.ChestId.Should().Be(chestId);
        result.State.Should().Be("EMPTY");
        result.Items.Should().BeEmpty();
        result.AlreadyEmpty.Should().BeTrue();
        inventoryRepository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_UnknownChest_ThrowsNotFound()
    {
        // Arrange
        var rewardRepository = new Mock<IRewardRepository>();
        rewardRepository
            .Setup(repository =>
                repository.GetByRewardKeyForUpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((RewardEntity?)null);
        var handler = new ClaimChestContentsCommandHandler(
            rewardRepository.Object,
            new Mock<IInventoryRepository>().Object,
            new FixedClock()
        );

        // Act
        Func<Task> action = () =>
            handler.Handle(
                new ClaimChestContentsCommand(Guid.NewGuid(), Guid.NewGuid()),
                TestContext.Current.CancellationToken
            );

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Chest '*");
    }

    [Fact]
    public async Task Handle_UnknownInventory_ThrowsNotFoundAndKeepsTheChestFilled()
    {
        // Arrange
        Guid chestId = Guid.NewGuid();
        RewardEntity reward = CreateReward(chestId, RewardStatus.Pending);
        RewardItem sword = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        Mock<IRewardRepository> rewardRepository = CreateRewardRepository(reward, sword);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Inventory?)null);
        var handler = new ClaimChestContentsCommandHandler(
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock()
        );

        // Act
        Func<Task> action = () =>
            handler.Handle(
                new ClaimChestContentsCommand(Guid.NewGuid(), chestId),
                TestContext.Current.CancellationToken
            );

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Inventory *");
        reward.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task Handle_NotEnoughCapacity_ThrowsWithoutAddingAnyItem()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        RewardEntity reward = CreateReward(chestId, RewardStatus.Pending);
        RewardItem sword = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 2);
        Inventory inventory = CreateInventory(heroId, itemCapacity: 1);
        Mock<IRewardRepository> rewardRepository = CreateRewardRepository(reward, sword);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        var handler = new ClaimChestContentsCommandHandler(
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock()
        );

        // Act
        Func<Task> action = () =>
            handler.Handle(
                new ClaimChestContentsCommand(heroId, chestId),
                TestContext.Current.CancellationToken
            );

        // Assert
        await action
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*item capacity of 1*");
        reward.Status.Should().Be("PENDING");
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public void Validate_EmptyIdentifiers_ReturnsAnErrorForEachIdentifier()
    {
        // Arrange
        var validator = new ClaimChestContentsCommandValidator();

        // Act
        var result = validator.Validate(new ClaimChestContentsCommand(Guid.Empty, Guid.Empty));

        // Assert
        result
            .Errors.Select(error => error.PropertyName)
            .Should()
            .BeEquivalentTo([
                nameof(ClaimChestContentsCommand.HeroId),
                nameof(ClaimChestContentsCommand.ChestId),
            ]);
    }

    private static Mock<IRewardRepository> CreateRewardRepository(
        RewardEntity reward,
        params RewardItem[] rewardItems
    )
    {
        var repository = new Mock<IRewardRepository>();
        repository
            .Setup(repository =>
                repository.GetByRewardKeyForUpdateAsync(
                    reward.RewardKey,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(reward);
        repository
            .Setup(repository =>
                repository.GetItemsByRewardIdAsync(reward.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(rewardItems);
        return repository;
    }

    private static RewardEntity CreateReward(Guid chestId, RewardStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            RewardKey = Chest.CreateRewardKey(chestId),
            Status = status.ToCode(),
        };

    private static RewardItem CreateRewardItem(
        RewardEntity reward,
        string category,
        bool stackable,
        int quantity
    )
    {
        Guid itemId = Guid.NewGuid();
        return new RewardItem
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            Reward = reward,
            ItemId = itemId,
            Item = new Item
            {
                Id = itemId,
                Stackable = stackable,
                Category = new Category { Label = category },
            },
            Quantity = quantity,
        };
    }

    private static Inventory CreateInventory(Guid heroId, int itemCapacity = 40) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
        };

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }
}
