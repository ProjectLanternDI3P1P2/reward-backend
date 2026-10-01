using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed class ClaimChestContentsCommandHandler(
    IChestLootRepository chestLootRepository,
    IInventoryRepository inventoryRepository,
    IClock clock
) : IRequestHandler<ClaimChestContentsCommand, ClaimChestContentsResult>
{
    public async Task<ClaimChestContentsResult> Handle(
        ClaimChestContentsCommand request,
        CancellationToken cancellationToken
    )
    {
        // Reusing the generation lock serializes concurrent claims of the same chest,
        // and a claim with a generation still in progress: the second caller waits,
        // then sees the chest already empty.
        await chestLootRepository.AcquireGenerationLockAsync(
            request.DungeonRunId,
            request.ChestId,
            cancellationToken
        );

        ChestLootGeneration generation =
            await chestLootRepository.GetGenerationAsync(
                request.DungeonRunId,
                request.ChestId,
                cancellationToken
            )
            ?? throw new KeyNotFoundException(
                $"Chest '{request.ChestId}' of run '{request.DungeonRunId}' was not found."
            );
        RewardEntity reward = generation.Reward;

        // Loot is generated without a recipient and the hero who claims it becomes its
        // owner, so a reward that already has a hero means the chest is empty.
        if (reward.HeroId != Guid.Empty)
        {
            return new ClaimChestContentsResult(
                request.ChestId,
                ChestState.Empty.ToCode(),
                [],
                true
            );
        }

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        // Every reward must fit: the inventory throws on the first one that does not, and
        // the command transaction then rolls back so the chest keeps all its rewards.
        DateTimeOffset now = clock.UtcNow;
        var itemInstances = new List<ItemInstance>();
        foreach (RewardItem rewardItem in reward.Items.OrderBy(rewardItem => rewardItem.ItemId))
        {
            Item item =
                await inventoryRepository.GetItemByIdAsync(rewardItem.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Item '{rewardItem.ItemId}' was not found.");

            List<ItemInstance> rewardInstances = CreateItemInstances(
                reward,
                rewardItem,
                item,
                inventory,
                now
            );
            foreach (ItemInstance itemInstance in rewardInstances)
            {
                inventory.Receive(itemInstance);
            }

            rewardItem.ItemInstanceId = rewardInstances[0].Id;
            itemInstances.AddRange(rewardInstances);
        }

        foreach (ItemInstance itemInstance in itemInstances)
        {
            inventoryRepository.AddItemInstance(itemInstance);
        }

        reward.HeroId = request.HeroId;

        return new ClaimChestContentsResult(
            request.ChestId,
            ChestState.Empty.ToCode(),
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

    // A stackable reward becomes one stack; any other reward takes one inventory
    // slot per unit, as items added one by one do.
    private static List<ItemInstance> CreateItemInstances(
        RewardEntity reward,
        RewardItem rewardItem,
        Item item,
        Inventory inventory,
        DateTimeOffset now
    )
    {
        if (item.Stackable)
        {
            return
            [
                CreateItemInstance(
                    reward,
                    rewardItem,
                    item,
                    inventory,
                    rewardItem.Quantity,
                    0,
                    now
                ),
            ];
        }

        return Enumerable
            .Range(0, rewardItem.Quantity)
            .Select(unit => CreateItemInstance(reward, rewardItem, item, inventory, 1, unit, now))
            .ToList();
    }

    // The idempotency key is unique per unit, so the database also refuses a second
    // transfer of the same reward item.
    private static ItemInstance CreateItemInstance(
        RewardEntity reward,
        RewardItem rewardItem,
        Item item,
        Inventory inventory,
        int quantity,
        int unit,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            InventoryId = inventory.Id,
            Item = item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = quantity,
            IdempotencyKey = $"{reward.RewardKey}:{rewardItem.Id}:{unit}",
            CreatedAt = now,
            UpdatedAt = now,
        };
}
