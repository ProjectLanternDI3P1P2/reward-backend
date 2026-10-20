using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.EquipItem;
using Reward.Application.Ports;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class EquipItemCommandHandlerTests
{
    private static readonly Guid WarriorHeroId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_EligibleSword_ReplacesRightHandEquipmentAndMarksItEquipped()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        ItemInstance sword = CreateItemInstance("SWORD");
        ItemInstance oldSword = CreateItemInstance(
            "SWORD",
            status: ItemInstanceStatus.Equipped,
            equipment: [CreateEquipment(rightHand.Id)]
        );
        var repository = CreateRepository(sword, oldSword);
        SetupSlots(repository, rightHand);
        var handler = CreateHandler(repository);

        await handler.Handle(
            new EquipItemCommand(WarriorHeroId, sword.Id, rightHand.Id),
            TestContext.Current.CancellationToken
        );

        sword.Status.Should().Be(ItemInstanceStatus.Equipped);
        oldSword.Status.Should().Be(ItemInstanceStatus.Available);
        repository.Verify(
            repository => repository.RemoveEquipment(oldSword.Equipment.Single()),
            Times.Once
        );
        repository.Verify(
            repository =>
                repository.AddEquipment(
                    It.Is<Equipment>(equipment =>
                        equipment.ItemInstanceId == sword.Id && equipment.SlotId == rightHand.Id
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_TwoHandedSword_ReplacesBothHandsAndOccupiesBothSlots()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        EquipmentSlot leftHand = CreateSlot("LEFT_HAND");
        ItemInstance twoHandedSword = CreateItemInstance("TWO_HANDED_SWORD");
        ItemInstance shield = CreateItemInstance(
            "SHIELD",
            status: ItemInstanceStatus.Equipped,
            equipment: [CreateEquipment(leftHand.Id)]
        );
        ItemInstance sword = CreateItemInstance(
            "SWORD",
            status: ItemInstanceStatus.Equipped,
            equipment: [CreateEquipment(rightHand.Id)]
        );
        var repository = CreateRepository(twoHandedSword, shield, sword);
        SetupSlots(repository, rightHand, leftHand);
        var handler = CreateHandler(repository);

        await handler.Handle(
            new EquipItemCommand(WarriorHeroId, twoHandedSword.Id, rightHand.Id),
            TestContext.Current.CancellationToken
        );

        shield.Status.Should().Be(ItemInstanceStatus.Available);
        sword.Status.Should().Be(ItemInstanceStatus.Available);
        repository.Verify(
            repository => repository.RemoveEquipment(It.IsAny<Equipment>()),
            Times.Exactly(2)
        );
        repository.Verify(
            repository =>
                repository.AddEquipment(
                    It.Is<Equipment>(e => e.ItemInstanceId == twoHandedSword.Id)
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_Ring_OnlyOccupiesTheSelectedJewelrySlot()
    {
        EquipmentSlot jewelry1 = CreateSlot("JEWELRY_1");
        EquipmentSlot jewelry2 = CreateSlot("JEWELRY_2");
        ItemInstance ring = CreateItemInstance("RING");
        var repository = CreateRepository(ring);
        SetupSlots(repository, jewelry1, jewelry2);
        var handler = CreateHandler(repository);

        await handler.Handle(
            new EquipItemCommand(WarriorHeroId, ring.Id, jewelry2.Id),
            TestContext.Current.CancellationToken
        );

        repository.Verify(
            repository =>
                repository.AddEquipment(
                    It.Is<Equipment>(equipment => equipment.SlotId == jewelry2.Id)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReservedItem_RejectsTheEquipment()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        ItemInstance item = CreateItemInstance("SWORD", status: ItemInstanceStatus.Reserved);
        var repository = CreateRepository(item);
        var handler = CreateHandler(repository);

        Func<Task> action = () =>
            handler.Handle(
                new EquipItemCommand(WarriorHeroId, item.Id, rightHand.Id),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
        repository.Verify(
            repository => repository.AddEquipment(It.IsAny<Equipment>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ListedItem_RejectsTheEquipment()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        ItemInstance item = CreateItemInstance("SWORD");
        var repository = CreateRepository(item, isListed: true);
        var handler = CreateHandler(repository);

        Func<Task> action = () =>
            handler.Handle(
                new EquipItemCommand(WarriorHeroId, item.Id, rightHand.Id),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
        repository.Verify(
            repository => repository.AddEquipment(It.IsAny<Equipment>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_HeroLevelTooLow_RejectsTheEquipment()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        ItemInstance item = CreateItemInstance("SWORD", levelRequired: 4);
        var repository = CreateRepository(item);
        var handler = CreateHandler(repository);

        Func<Task> action = () =>
            handler.Handle(
                new EquipItemCommand(WarriorHeroId, item.Id, rightHand.Id),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_HeroClassIsNotAllowed_RejectsTheEquipment()
    {
        EquipmentSlot rightHand = CreateSlot("RIGHT_HAND");
        ItemInstance item = CreateItemInstance("SWORD", classTags: ["MAGE"]);
        var repository = CreateRepository(item);
        var handler = CreateHandler(repository);

        Func<Task> action = () =>
            handler.Handle(
                new EquipItemCommand(WarriorHeroId, item.Id, rightHand.Id),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    private static EquipItemCommandHandler CreateHandler(Mock<IInventoryRepository> repository)
    {
        var profiles = new Mock<IHeroProfileClient>();
        profiles
            .Setup(client => client.GetByHeroIdAsync(WarriorHeroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HeroProfile(
                    WarriorHeroId,
                    3,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WARRIOR" }
                )
            );
        return new EquipItemCommandHandler(repository.Object, profiles.Object, new FixedClock());
    }

    private static Mock<IInventoryRepository> CreateRepository(
        ItemInstance item,
        ItemInstance? otherItem = null,
        ItemInstance? anotherItem = null,
        bool isListed = false
    )
    {
        var items = new[] { item, otherItem, anotherItem }
            .Where(item => item is not null)
            .Cast<ItemInstance>()
            .ToList();
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(WarriorHeroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new Inventory
                {
                    Id = Guid.NewGuid(),
                    HeroId = WarriorHeroId,
                    ItemInstances = items,
                }
            );
        repository
            .Setup(repository =>
                repository.HasActiveMarketplaceListingAsync(item.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(isListed);
        return repository;
    }

    private static void SetupSlots(
        Mock<IInventoryRepository> repository,
        params EquipmentSlot[] slots
    ) =>
        repository
            .Setup(repository =>
                repository.GetEquipmentSlotsByNamesAsync(
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(slots);

    private static EquipmentSlot CreateSlot(string name) =>
        new() { Id = Guid.NewGuid(), Name = name };

    private static Equipment CreateEquipment(Guid slotId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SlotId = slotId,
            Quantity = 1,
        };

    private static ItemInstance CreateItemInstance(
        string category,
        ItemInstanceStatus status = ItemInstanceStatus.Available,
        int levelRequired = 1,
        IReadOnlyCollection<string>? classTags = null,
        ICollection<Equipment>? equipment = null
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Status = status,
            Item = new Item
            {
                Category = new Category { Label = category },
                LevelRequired = levelRequired,
                ClassTags = (classTags ?? [])
                    .Select(tag => new ItemClassTag { ClassTag = new ClassTag { Label = tag } })
                    .ToList(),
            },
            Equipment = equipment ?? [],
        };

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
