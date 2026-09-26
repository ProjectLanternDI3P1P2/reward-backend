using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.RemoveItemFromInventory;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class RemoveItemFromInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_AvailableOwnedItem_RemovesTheItem()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Status = ItemInstanceStatus.Available,
        };
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new RemoveItemFromInventoryCommandHandler(repository.Object);

        await handler.Handle(
            new RemoveItemFromInventoryCommand(heroId, itemInstance.Id),
            TestContext.Current.CancellationToken
        );

        repository.Verify(repository => repository.RemoveItemInstance(itemInstance), Times.Once);
    }

    [Fact]
    public async Task Handle_EquippedOwnedItem_RemovesTheItemAndLetsTheRepositoryUnequipIt()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Status = ItemInstanceStatus.Equipped,
            Equipment = [new Equipment { Id = Guid.NewGuid(), HeroId = heroId }],
        };
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new RemoveItemFromInventoryCommandHandler(repository.Object);

        await handler.Handle(
            new RemoveItemFromInventoryCommand(heroId, itemInstance.Id),
            TestContext.Current.CancellationToken
        );

        repository.Verify(repository => repository.RemoveItemInstance(itemInstance), Times.Once);
    }

    [Fact]
    public async Task Handle_ReservedOwnedItem_ThrowsConflictAndDoesNotRemoveIt()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Status = ItemInstanceStatus.Reserved,
        };
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new RemoveItemFromInventoryCommandHandler(repository.Object);

        Func<Task> action = () =>
            handler.Handle(
                new RemoveItemFromInventoryCommand(heroId, itemInstance.Id),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
        repository.Verify(
            repository => repository.RemoveItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ItemWasAlreadyRemoved_CompletesWithoutAnotherRemoval()
    {
        Guid heroId = Guid.NewGuid();
        var repository = CreateRepository(heroId);
        var handler = new RemoveItemFromInventoryCommandHandler(repository.Object);

        await handler.Handle(
            new RemoveItemFromInventoryCommand(heroId, Guid.NewGuid()),
            TestContext.Current.CancellationToken
        );

        repository.Verify(
            repository => repository.RemoveItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_InventoryDoesNotBelongToHero_ThrowsNotFound()
    {
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Inventory?)null);
        var handler = new RemoveItemFromInventoryCommandHandler(repository.Object);

        Func<Task> action = () =>
            handler.Handle(
                new RemoveItemFromInventoryCommand(Guid.NewGuid(), Guid.NewGuid()),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static Mock<IInventoryRepository> CreateRepository(
        Guid heroId,
        params ItemInstance[] items
    )
    {
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new Inventory
                {
                    Id = Guid.NewGuid(),
                    HeroId = heroId,
                    ItemInstances = items,
                }
            );
        return repository;
    }
}
