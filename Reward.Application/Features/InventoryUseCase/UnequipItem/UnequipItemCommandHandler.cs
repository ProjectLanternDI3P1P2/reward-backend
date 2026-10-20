using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Application.Features.InventoryUseCase.UnequipItem;

public sealed class UnequipItemCommandHandler(IInventoryRepository repository, IClock clock)
    : IRequestHandler<UnequipItemCommand>
{
    public async Task Handle(UnequipItemCommand request, CancellationToken cancellationToken)
    {
        Inventory inventory =
            await repository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        ItemInstance itemInstance =
            inventory.ItemInstances.SingleOrDefault(instance =>
                instance.Id == request.ItemInstanceId
            )
            ?? throw new KeyNotFoundException(
                $"Item instance '{request.ItemInstanceId}' was not found."
            );

        if (itemInstance.Status != ItemInstanceStatus.Equipped)
        {
            return;
        }

        foreach (Equipment equipment in itemInstance.Equipment.ToList())
        {
            repository.RemoveEquipment(equipment);
        }

        itemInstance.Status = ItemInstanceStatus.Available;
        itemInstance.UpdatedAt = clock.UtcNow;
    }
}
