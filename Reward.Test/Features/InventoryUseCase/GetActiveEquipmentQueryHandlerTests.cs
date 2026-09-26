using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.GetActiveEquipment;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class GetActiveEquipmentQueryHandlerTests
{
    [Fact]
    public async Task Handle_HeroExists_ReturnsEquippedItemsModifiersAndEmptySlots()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid weaponSlotId = Guid.NewGuid();
        Guid helmetSlotId = Guid.NewGuid();
        Guid equippedInstanceId = Guid.NewGuid();
        var snapshot = new ActiveEquipmentSnapshot(
            heroId,
            [
                new ActiveEquipmentSlotSnapshot(
                    new EquipmentSlot { Id = helmetSlotId, Name = "HEAD" },
                    null
                ),
                new ActiveEquipmentSlotSnapshot(
                    new EquipmentSlot { Id = weaponSlotId, Name = "MAIN_HAND" },
                    CreateEquipment(heroId, weaponSlotId, equippedInstanceId)
                ),
            ]
        );
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetActiveEquipmentByHeroIdAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(snapshot);
        var handler = new GetActiveEquipmentQueryHandler(repository.Object);

        // Act
        ActiveEquipment? result = await handler.Handle(
            new GetActiveEquipmentQuery(heroId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                new ActiveEquipment(
                    heroId,
                    [
                        new ActiveEquipmentSlot(helmetSlotId, "HEAD", null),
                        new ActiveEquipmentSlot(
                            weaponSlotId,
                            "MAIN_HAND",
                            new EquippedItem(
                                equippedInstanceId,
                                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                                "WEAPON",
                                "Obsidian Blade",
                                "Epic",
                                [new CombatModifier("Brutality", "ATTACK", 12.5m, "FLAT")]
                            )
                        ),
                    ]
                )
            );
    }

    [Fact]
    public async Task Handle_HeroDoesNotExist_ReturnsNull()
    {
        // Arrange
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetActiveEquipmentByHeroIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ActiveEquipmentSnapshot?)null);
        var handler = new GetActiveEquipmentQueryHandler(repository.Object);

        // Act
        ActiveEquipment? result = await handler.Handle(
            new GetActiveEquipmentQuery(Guid.NewGuid()),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().BeNull();
    }

    private static Equipment CreateEquipment(Guid heroId, Guid slotId, Guid instanceId) =>
        new()
        {
            HeroId = heroId,
            SlotId = slotId,
            ItemInstance = new ItemInstance
            {
                Id = instanceId,
                Item = new Item
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Obsidian Blade",
                    Category = new Category { Label = "WEAPON" },
                    Rarity = new Rarity { Label = "Epic" },
                    Modifiers =
                    [
                        new ItemModifier
                        {
                            Modifier = new Modifier
                            {
                                Id = Guid.NewGuid(),
                                Name = "Brutality",
                                Stat = "ATTACK",
                                Value = 12.5m,
                                Type = "FLAT",
                            },
                        },
                    ],
                },
            },
        };
}
