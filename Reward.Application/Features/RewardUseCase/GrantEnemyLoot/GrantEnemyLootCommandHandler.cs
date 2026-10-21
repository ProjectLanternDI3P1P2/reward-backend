using MediatR;
using Reward.Application.Features.RewardUseCase.GrantReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;
using ILogger = Serilog.ILogger;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.RewardUseCase.GrantEnemyLoot;

public sealed class GrantEnemyLootCommandHandler(
    ILootTableRepository lootTableRepository,
    IRewardRepository rewardRepository,
    IInventoryRepository inventoryRepository,
    LootSelectionService lootSelectionService,
    BossLootSelectionService bossLootSelectionService,
    IClock clock,
    ILogger logger
) : IRequestHandler<GrantEnemyLootCommand, GrantEnemyLootResult>
{
    public async Task<GrantEnemyLootResult> Handle(
        GrantEnemyLootCommand request,
        CancellationToken cancellationToken
    )
    {
        // Only a victory earns a combat reward: nothing is drawn or persisted otherwise.
        if (request.Outcome != CombatOutcome.Won)
        {
            logger.Information(
                "No loot generated for {EnemyType} {EnemyId}: the combat ended with {Outcome}",
                request.EnemyType,
                request.EnemyId,
                request.Outcome
            );
            return new GrantEnemyLootResult(request.EnemyId, [], []);
        }

        // Heroes are handled in a stable order so concurrent deliveries lock their
        // inventories in the same order.
        IReadOnlyList<ActivityParticipant> recipients = RewardEligibilityPolicy
            .GetEligibleParticipants(request.Participants)
            .OrderBy(participant => participant.HeroId)
            .ToList();

        // A replay is answered from the persisted rewards alone, so it still works when the
        // loot table was changed or removed since the first delivery.
        var heroLoots = new List<GrantedHeroLoot>();
        var pendingHeroIds = new List<Guid>();
        foreach (Guid heroId in recipients.Select(recipient => recipient.HeroId))
        {
            RewardEntity? existing = await rewardRepository.GetByKeyAsync(
                RewardEntity.CreateKey(request.EnemyType, request.EnemyId, heroId),
                cancellationToken
            );
            if (existing is null)
            {
                pendingHeroIds.Add(heroId);
            }
            else
            {
                heroLoots.Add(AcknowledgeDuplicate(existing, request, heroId));
            }
        }

        var fullInventoryHeroIds = new List<Guid>();
        if (pendingHeroIds.Count > 0)
        {
            (LootTable table, RewardSource source) = await LoadLootSourceAsync(
                request,
                cancellationToken
            );

            foreach (Guid heroId in pendingHeroIds)
            {
                GrantedHeroLoot? heroLoot = await TryGrantToHeroAsync(
                    request,
                    table,
                    source,
                    heroId,
                    cancellationToken
                );
                if (heroLoot is null)
                {
                    fullInventoryHeroIds.Add(heroId);
                }
                else
                {
                    heroLoots.Add(heroLoot);
                }
            }
        }

        return new GrantEnemyLootResult(
            request.EnemyId,
            heroLoots.OrderBy(heroLoot => heroLoot.HeroId).ToList(),
            fullInventoryHeroIds
        );
    }

    private async Task<(LootTable Table, RewardSource Source)> LoadLootSourceAsync(
        GrantEnemyLootCommand request,
        CancellationToken cancellationToken
    )
    {
        string sourceName = request.EnemyType.ToCode();
        LootTable table =
            await lootTableRepository.GetByIdAsync(request.LootTableId, cancellationToken)
            ?? throw new LootTableNotFoundException(
                $"Loot table '{request.LootTableId}' was not found."
            );

        // The enemy's table must be dedicated to its kind, so a chest table never drops
        // from a monster and a monster table never drops from a boss.
        if (table.SourceType != sourceName)
        {
            throw new InvalidLootTableException(
                $"Loot table '{table.Id}' is configured for '{table.SourceType}' and cannot reward a {sourceName} enemy."
            );
        }

        RewardSource source =
            await rewardRepository.GetSourceByNameAsync(sourceName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"The reward source '{sourceName}' is not configured."
            );

        return (table, source);
    }

    // Returns null when the hero inventory cannot hold all of the loot: nothing is then
    // drawn into it, persisted or left half applied.
    private async Task<GrantedHeroLoot?> TryGrantToHeroAsync(
        GrantEnemyLootCommand request,
        LootTable table,
        RewardSource source,
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        string rewardKey = RewardEntity.CreateKey(request.EnemyType, request.EnemyId, heroId);

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(heroId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory for hero '{heroId}' was not found.");

        // A concurrent delivery of the same operation may have been granted while this one
        // was waiting for the inventory lock.
        RewardEntity? existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request, heroId);
        }

        // Every selected item comes from the enemy's loot table, by construction.
        IReadOnlyList<SelectedLootItem> loot = SelectLoot(request.EnemyType, table);
        DateTimeOffset now = clock.UtcNow;
        if (!CanReceive(inventory, loot, now))
        {
            logger.Warning(
                "Enemy loot {RewardKey} skipped: the inventory of hero {HeroId} is full",
                rewardKey,
                heroId
            );
            return null;
        }

        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            RunId = request.RunId,
            RewardSourceId = source.Id,
            RewardSource = source,
            Type = RewardType.Item,
            Status = RewardStatus.Pending,
            RewardKey = rewardKey,
            CreatedAt = now,
        };
        foreach (SelectedLootItem selected in loot)
        {
            ItemInstance itemInstance = reward.CreditItem(
                inventory,
                selected.Item,
                selected.Quantity,
                now
            );
            inventoryRepository.AddItemInstance(itemInstance);
        }

        // The reward and its items are committed in the same transaction, so a persisted
        // reward is always fully applied.
        reward.Status = RewardStatus.Applied;
        rewardRepository.Add(reward);

        logger.Information(
            "Enemy loot {RewardKey} granted to hero {HeroId} for {EnemyType} {EnemyId} from loot table {LootTableId}",
            rewardKey,
            heroId,
            request.EnemyType,
            request.EnemyId,
            table.Id
        );

        return ToHeroLoot(reward, alreadyGranted: false);
    }

    // A boss always drops a high-rarity equipment item on top of its regular draws.
    private IReadOnlyList<SelectedLootItem> SelectLoot(
        RewardSourceType enemyType,
        LootTable table
    ) =>
        enemyType == RewardSourceType.Boss
            ? bossLootSelectionService.Select(table)
            : lootSelectionService.Select(table);

    // Credits the loot to a throwaway copy of the inventory first. The real inventory is a
    // tracked entity that crediting would change, so a hero that cannot hold everything
    // would otherwise keep the part that fitted.
    private static bool CanReceive(
        Inventory inventory,
        IReadOnlyList<SelectedLootItem> loot,
        DateTimeOffset now
    )
    {
        var inventoryCopy = new Inventory
        {
            Id = inventory.Id,
            HeroId = inventory.HeroId,
            ItemCapacity = inventory.ItemCapacity,
            PotionCapacity = inventory.PotionCapacity,
            ItemInstances = inventory.ItemInstances.ToList(),
        };
        var dryRunReward = new RewardEntity { Id = Guid.NewGuid() };

        try
        {
            foreach (SelectedLootItem selected in loot)
            {
                dryRunReward.CreditItem(inventoryCopy, selected.Item, selected.Quantity, now);
            }

            return true;
        }
        catch (InventoryCapacityExceededException)
        {
            return false;
        }
    }

    private GrantedHeroLoot AcknowledgeDuplicate(
        RewardEntity existing,
        GrantEnemyLootCommand request,
        Guid heroId
    )
    {
        if (existing.Status != RewardStatus.Applied)
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' exists but is not applied."
            );
        }

        if (existing.RunId != request.RunId)
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' was already granted for another run."
            );
        }

        logger.Information(
            "Duplicate enemy loot operation {RewardKey} ignored for hero {HeroId}",
            existing.RewardKey,
            heroId
        );

        return ToHeroLoot(existing, alreadyGranted: true);
    }

    private static GrantedHeroLoot ToHeroLoot(RewardEntity reward, bool alreadyGranted) =>
        new(
            reward.HeroId,
            reward.Id,
            reward
                .Items.Select(item => new GrantedRewardItem(
                    item.ItemId,
                    item.ItemInstanceId,
                    item.Quantity
                ))
                .ToList(),
            alreadyGranted
        );
}
