using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;

namespace Reward.Application.Features.InventoryUseCase.RemoveItemFromInventory;

public sealed class RemoveItemFromInventoryCommandHandler(IInventoryRepository repository)
    : IRequestHandler<RemoveItemFromInventoryCommand>
{
    public async Task Handle(
        RemoveItemFromInventoryCommand request,
        CancellationToken cancellationToken
    )
    {
        Inventory? inventory = await repository.GetByHeroIdForUpdateAsync(
            request.HeroId,
            cancellationToken
        );
        if (inventory is null)
        {
            throw new KeyNotFoundException($"Inventory for hero '{request.HeroId}' was not found.");
        }

        ItemInstance? itemInstance = inventory.ItemInstances.SingleOrDefault(instance =>
            instance.Id == request.ItemInstanceId
        );
        if (itemInstance is null)
        {
            return;
        }

        if (itemInstance.Status == ItemInstanceStatus.Reserved)
        {
            throw new InvalidOperationException("A reserved item cannot be removed.");
        }

        repository.RemoveItemInstance(itemInstance);
    }
}
