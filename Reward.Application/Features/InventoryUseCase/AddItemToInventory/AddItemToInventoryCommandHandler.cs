using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Application.Features.InventoryUseCase.AddItemToInventory;

public sealed class AddItemToInventoryCommandHandler(IInventoryRepository repository, IClock clock)
    : IRequestHandler<AddItemToInventoryCommand, AddItemToInventoryResult>
{
    public async Task<AddItemToInventoryResult> Handle(
        AddItemToInventoryCommand request,
        CancellationToken cancellationToken
    )
    {
        ItemInstance? existing = await repository.GetItemInstanceByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            return new AddItemToInventoryResult(existing.Id, true);
        }

        Inventory? inventory = await repository.GetByHeroIdForUpdateAsync(
            request.HeroId,
            cancellationToken
        );
        if (inventory is null)
        {
            throw new KeyNotFoundException($"Inventory for hero '{request.HeroId}' was not found.");
        }

        existing = await repository.GetItemInstanceByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            return new AddItemToInventoryResult(existing.Id, true);
        }

        Item? item = await repository.GetItemByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
        {
            throw new KeyNotFoundException($"Item '{request.ItemId}' was not found.");
        }

        DateTimeOffset now = clock.UtcNow;
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            InventoryId = inventory.Id,
            Item = item,
            Inventory = inventory,
            Status = "AVAILABLE",
            Quantity = 1,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = now,
            UpdatedAt = now,
        };

        inventory.Receive(itemInstance);
        repository.AddItemInstance(itemInstance);

        return new AddItemToInventoryResult(itemInstance.Id, false);
    }
}
