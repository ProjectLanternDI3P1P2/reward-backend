using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.AddItemToInventory;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class AddItemToInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_SpaceAvailable_AddsAvailableItemToTheHeroInventory()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        var inventory = CreateInventory(heroId, itemCapacity: 40, potionCapacity: 20);
        Item item = CreateItem(itemId, "WEAPON");
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetItemInstanceByIdempotencyKeyAsync(
                    "reward-1",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ItemInstance?)null);
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        repository
            .Setup(repository => repository.GetItemByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var handler = new AddItemToInventoryCommandHandler(repository.Object, new FixedClock());

        // Act
        AddItemToInventoryResult result = await handler.Handle(
            new AddItemToInventoryCommand(heroId, itemId, "reward-1"),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyExists.Should().BeFalse();
        inventory
            .ItemInstances.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    ItemId = itemId,
                    InventoryId = inventory.Id,
                    Status = "AVAILABLE",
                    Quantity = 1,
                    IdempotencyKey = "reward-1",
                }
            );
        repository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_IdempotencyKeyAlreadyProcessed_ReturnsExistingItemWithoutAddingAnother()
    {
        // Arrange
        var existing = new ItemInstance { Id = Guid.NewGuid() };
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetItemInstanceByIdempotencyKeyAsync(
                    "reward-1",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existing);
        var handler = new AddItemToInventoryCommandHandler(repository.Object, new FixedClock());

        // Act
        AddItemToInventoryResult result = await handler.Handle(
            new AddItemToInventoryCommand(Guid.NewGuid(), Guid.NewGuid(), "reward-1"),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().Be(new AddItemToInventoryResult(existing.Id, true));
        repository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        repository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ItemCapacityReached_DoesNotAddAnItem()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        var inventory = CreateInventory(heroId, itemCapacity: 40, potionCapacity: 20);
        inventory.ItemInstances = Enumerable
            .Range(0, Inventory.MaximumItemSlots)
            .Select(_ => CreateItemInstance("WEAPON"))
            .ToList();
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetItemInstanceByIdempotencyKeyAsync(
                    "reward-1",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ItemInstance?)null);
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        repository
            .Setup(repository => repository.GetItemByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(itemId, "WEAPON"));
        var handler = new AddItemToInventoryCommandHandler(repository.Object, new FixedClock());

        // Act
        Func<Task> action = () =>
            handler.Handle(
                new AddItemToInventoryCommand(heroId, itemId, "reward-1"),
                TestContext.Current.CancellationToken
            );

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        inventory.ItemInstances.Should().HaveCount(Inventory.MaximumItemSlots);
        repository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public void Receive_PotionCapacityReached_DoesNotAddAPotion()
    {
        // Arrange
        var inventory = CreateInventory(Guid.NewGuid(), itemCapacity: 40, potionCapacity: 20);
        inventory.ItemInstances = Enumerable
            .Range(0, Inventory.MaximumPotionSlots)
            .Select(_ => CreateItemInstance("POTION"))
            .ToList();

        // Act
        Action action = () => inventory.Receive(CreateItemInstance("POTION"));

        // Assert
        action.Should().Throw<InvalidOperationException>();
        inventory.ItemInstances.Should().HaveCount(Inventory.MaximumPotionSlots);
    }

    private static Inventory CreateInventory(Guid heroId, int itemCapacity, int potionCapacity) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = potionCapacity,
        };

    private static Item CreateItem(Guid id, string category) =>
        new()
        {
            Id = id,
            Category = new Category { Label = category },
        };

    private static ItemInstance CreateItemInstance(string category) =>
        new() { Item = CreateItem(Guid.NewGuid(), category) };

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    }
}
