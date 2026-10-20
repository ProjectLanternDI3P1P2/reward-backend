using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Application.Features.InventoryUseCase.ConsumeConsumable;

public sealed class ConsumeConsumableCommandHandler(IInventoryRepository repository, IClock clock)
    : IRequestHandler<ConsumeConsumableCommand, ConsumeConsumableResult>
{
    public async Task<ConsumeConsumableResult> Handle(
        ConsumeConsumableCommand request,
        CancellationToken cancellationToken
    )
    {
        ConsumableUse? existing = await repository.GetConsumableUseByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            return new ConsumeConsumableResult(
                existing.ItemInstanceId ?? request.ItemInstanceId,
                existing.RemainingQuantity,
                true
            );
        }

        Inventory inventory =
            await repository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        existing = await repository.GetConsumableUseByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            return new ConsumeConsumableResult(
                existing.ItemInstanceId ?? request.ItemInstanceId,
                existing.RemainingQuantity,
                true
            );
        }

        ItemInstance itemInstance =
            inventory.ItemInstances.SingleOrDefault(instance =>
                instance.Id == request.ItemInstanceId
            )
            ?? throw new KeyNotFoundException(
                $"Item instance '{request.ItemInstanceId}' was not found."
            );

        ItemCategory category = itemInstance.Item.Category.Label.ToItemCategory();
        if (!category.IsConsumable())
        {
            throw new InvalidOperationException("Only consumables can be consumed.");
        }

        if (itemInstance.Status == ItemInstanceStatus.Reserved)
        {
            throw new InvalidOperationException("A reserved consumable cannot be consumed.");
        }

        if (itemInstance.Quantity <= 0)
        {
            throw new InvalidOperationException("A consumable must have a positive quantity.");
        }

        int remainingQuantity = itemInstance.Quantity - 1;
        DateTimeOffset now = clock.UtcNow;
        var consumableUse = new ConsumableUse
        {
            Id = Guid.NewGuid(),
            HeroId = request.HeroId,
            ItemInstanceId = itemInstance.Id,
            IdempotencyKey = request.IdempotencyKey,
            RemainingQuantity = remainingQuantity,
            CreatedAt = now,
        };

        if (remainingQuantity == 0)
        {
            repository.RemoveItemInstance(itemInstance);
            consumableUse.ItemInstanceId = null;
        }
        else
        {
            itemInstance.Quantity = remainingQuantity;
            itemInstance.UpdatedAt = now;
        }

        repository.AddConsumableUse(consumableUse);
        return new ConsumeConsumableResult(itemInstance.Id, remainingQuantity, false);
    }
}
