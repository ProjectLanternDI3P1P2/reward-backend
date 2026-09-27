using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.UnequipItem;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class UnequipItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_EquippedItem_RemovesItsEquipmentAndMakesItAvailable()
    {
        Guid heroId = Guid.NewGuid();
        var firstEquipment = new Equipment { Id = Guid.NewGuid() };
        var secondEquipment = new Equipment { Id = Guid.NewGuid() };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Status = ItemInstanceStatus.Equipped,
            Equipment = [firstEquipment, secondEquipment],
        };
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new UnequipItemCommandHandler(repository.Object, new FixedClock());

        await handler.Handle(
            new UnequipItemCommand(heroId, itemInstance.Id),
            TestContext.Current.CancellationToken
        );

        itemInstance.Status.Should().Be(ItemInstanceStatus.Available);
        itemInstance.UpdatedAt.Should().Be(FixedClock.Now);
        repository.Verify(repository => repository.RemoveEquipment(firstEquipment), Times.Once);
        repository.Verify(repository => repository.RemoveEquipment(secondEquipment), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemIsAlreadyUnequipped_CompletesWithoutChangingIt()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Status = ItemInstanceStatus.Available,
        };
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new UnequipItemCommandHandler(repository.Object, new FixedClock());

        await handler.Handle(
            new UnequipItemCommand(heroId, itemInstance.Id),
            TestContext.Current.CancellationToken
        );

        itemInstance.Status.Should().Be(ItemInstanceStatus.Available);
        repository.Verify(
            repository => repository.RemoveEquipment(It.IsAny<Equipment>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ItemDoesNotBelongToHero_ThrowsNotFound()
    {
        Guid heroId = Guid.NewGuid();
        var repository = CreateRepository(heroId);
        var handler = new UnequipItemCommandHandler(repository.Object, new FixedClock());

        Func<Task> action = () =>
            handler.Handle(
                new UnequipItemCommand(heroId, Guid.NewGuid()),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static Mock<IInventoryRepository> CreateRepository(
        Guid heroId,
        params ItemInstance[] itemInstances
    )
    {
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new Inventory { HeroId = heroId, ItemInstances = itemInstances });
        return repository;
    }

    private sealed class FixedClock : IClock
    {
        public static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }
}
