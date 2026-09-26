using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.GetHeroInventory;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class GetHeroInventoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_InventoryExists_SeparatesConsumablesAndExposesItemState()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid equippedItemId = Guid.NewGuid();
        Guid reservedPotionId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemInstances =
            [
                CreateItemInstance(
                    equippedItemId,
                    "WEAPON",
                    "Obsidian Blade",
                    "Epic",
                    "AVAILABLE",
                    1,
                    heroId
                ),
                CreateItemInstance(
                    reservedPotionId,
                    "POTION",
                    "Health Potion",
                    "Common",
                    "RESERVED",
                    3
                ),
            ],
        };
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository => repository.GetByHeroIdAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        var handler = new GetHeroInventoryQueryHandler(repository.Object);

        // Act
        HeroInventory? result = await handler.Handle(
            new GetHeroInventoryQuery(heroId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().NotBeNull();
        result!.HeroId.Should().Be(heroId);
        result
            .Items.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new InventoryItem(
                    equippedItemId,
                    "WEAPON",
                    "Obsidian Blade",
                    "Epic",
                    "AVAILABLE",
                    1,
                    true,
                    false
                )
            );
        result
            .Consumables.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new InventoryItem(
                    reservedPotionId,
                    "POTION",
                    "Health Potion",
                    "Common",
                    "RESERVED",
                    3,
                    false,
                    true
                )
            );
    }

    [Fact]
    public async Task Handle_InventoryDoesNotExist_ReturnsNull()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository => repository.GetByHeroIdAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);
        var handler = new GetHeroInventoryQueryHandler(repository.Object);

        // Act
        HeroInventory? result = await handler.Handle(
            new GetHeroInventoryQuery(heroId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().BeNull();
    }

    private static ItemInstance CreateItemInstance(
        Guid id,
        string category,
        string name,
        string rarity,
        string status,
        int quantity,
        Guid? equippedHeroId = null
    ) =>
        new()
        {
            Id = id,
            Status = status,
            Quantity = quantity,
            Item = new Item
            {
                Name = name,
                Category = new Category { Label = category },
                Rarity = new Rarity { Label = rarity },
            },
            Equipment = equippedHeroId is null
                ? []
                : [new Equipment { HeroId = equippedHeroId.Value }],
        };
}
