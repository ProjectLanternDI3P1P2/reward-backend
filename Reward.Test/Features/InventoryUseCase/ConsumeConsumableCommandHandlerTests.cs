using FluentAssertions;
using Moq;
using Reward.Application.Features.InventoryUseCase.ConsumeConsumable;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Test.Features.InventoryUseCase;

public sealed class ConsumeConsumableCommandHandlerTests
{
    [Fact]
    public async Task Handle_AvailableConsumable_DecreasesQuantityByOne()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = CreateItemInstance(quantity: 2);
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new ConsumeConsumableCommandHandler(repository.Object, new FixedClock());

        ConsumeConsumableResult result = await handler.Handle(
            new ConsumeConsumableCommand(heroId, itemInstance.Id, "consume-1"),
            TestContext.Current.CancellationToken
        );

        result.Should().Be(new ConsumeConsumableResult(itemInstance.Id, 1, false));
        itemInstance.Quantity.Should().Be(1);
        repository.Verify(
            repository =>
                repository.AddConsumableUse(
                    It.Is<ConsumableUse>(use =>
                        use.ItemInstanceId == itemInstance.Id
                        && use.RemainingQuantity == 1
                        && use.IdempotencyKey == "consume-1"
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_LastConsumable_RemovesTheEmptyStack()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = CreateItemInstance(quantity: 1);
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new ConsumeConsumableCommandHandler(repository.Object, new FixedClock());

        ConsumeConsumableResult result = await handler.Handle(
            new ConsumeConsumableCommand(heroId, itemInstance.Id, "consume-1"),
            TestContext.Current.CancellationToken
        );

        result.RemainingQuantity.Should().Be(0);
        repository.Verify(repository => repository.RemoveItemInstance(itemInstance), Times.Once);
        repository.Verify(
            repository =>
                repository.AddConsumableUse(
                    It.Is<ConsumableUse>(use => use.ItemInstanceId == null)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_Retry_ReturnsPreviousResultWithoutConsumingAgain()
    {
        Guid heroId = Guid.NewGuid();
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetConsumableUseByIdempotencyKeyAsync(
                    "consume-1",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ConsumableUse { ItemInstanceId = Guid.NewGuid(), RemainingQuantity = 2 }
            );
        var handler = new ConsumeConsumableCommandHandler(repository.Object, new FixedClock());

        ConsumeConsumableResult result = await handler.Handle(
            new ConsumeConsumableCommand(heroId, Guid.NewGuid(), "consume-1"),
            TestContext.Current.CancellationToken
        );

        result.AlreadyConsumed.Should().BeTrue();
        repository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReservedConsumable_RejectsTheConsumption()
    {
        Guid heroId = Guid.NewGuid();
        var itemInstance = CreateItemInstance(quantity: 2, status: ItemInstanceStatus.Reserved);
        var repository = CreateRepository(heroId, itemInstance);
        var handler = new ConsumeConsumableCommandHandler(repository.Object, new FixedClock());

        Func<Task> action = () =>
            handler.Handle(
                new ConsumeConsumableCommand(heroId, itemInstance.Id, "consume-1"),
                TestContext.Current.CancellationToken
            );

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    private static Mock<IInventoryRepository> CreateRepository(
        Guid heroId,
        ItemInstance itemInstance
    )
    {
        var repository = new Mock<IInventoryRepository>();
        repository
            .Setup(repository =>
                repository.GetConsumableUseByIdempotencyKeyAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ConsumableUse?)null);
        repository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new Inventory { HeroId = heroId, ItemInstances = [itemInstance] });
        return repository;
    }

    private static ItemInstance CreateItemInstance(
        int quantity,
        ItemInstanceStatus status = ItemInstanceStatus.Available
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Quantity = quantity,
            Status = status,
            Item = new Item { Category = new Category { Label = "POTION" } },
        };

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    }
}
