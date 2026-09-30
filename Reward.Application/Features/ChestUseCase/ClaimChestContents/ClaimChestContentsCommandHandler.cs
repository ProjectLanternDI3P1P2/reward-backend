using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed class ClaimChestContentsCommandHandler(
    IRewardRepository rewardRepository,
    IInventoryRepository inventoryRepository,
    IClock clock
) : IRequestHandler<ClaimChestContentsCommand, ClaimChestContentsResult>
{
    public async Task<ClaimChestContentsResult> Handle(
        ClaimChestContentsCommand request,
        CancellationToken cancellationToken
    )
    {
        // Locking the reward first serializes concurrent claims of the same chest:
        // the second one waits, then sees the chest already empty.
        RewardEntity reward =
            await rewardRepository.GetByRewardKeyForUpdateAsync(
                Chest.CreateRewardKey(request.ChestId),
                cancellationToken
            ) ?? throw new KeyNotFoundException($"Chest '{request.ChestId}' was not found.");

        IReadOnlyList<RewardItem> rewardItems = await rewardRepository.GetItemsByRewardIdAsync(
            reward.Id,
            cancellationToken
        );
        var chest = new Chest(request.ChestId, reward, rewardItems);

        if (chest.State == ChestState.Empty)
        {
            return new ClaimChestContentsResult(chest.ChestId, chest.State.ToCode(), [], true);
        }

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        IReadOnlyList<ItemInstance> itemInstances = chest.TransferTo(inventory, clock.UtcNow);
        foreach (ItemInstance itemInstance in itemInstances)
        {
            inventoryRepository.AddItemInstance(itemInstance);
        }

        return new ClaimChestContentsResult(
            chest.ChestId,
            chest.State.ToCode(),
            itemInstances
                .Select(itemInstance => new ClaimedChestItem(
                    itemInstance.Id,
                    itemInstance.ItemId,
                    itemInstance.Quantity
                ))
                .ToList(),
            false
        );
    }
}
