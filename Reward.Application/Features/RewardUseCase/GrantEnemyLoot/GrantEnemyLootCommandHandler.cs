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
            return new GrantEnemyLootResult(request.EnemyId, []);
        }

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

        // Heroes are handled in a stable order so concurrent deliveries lock their
        // inventories in the same order.
        IEnumerable<ActivityParticipant> recipients = RewardEligibilityPolicy
            .GetEligibleParticipants(request.Participants)
            .OrderBy(participant => participant.HeroId);

        var heroLoots = new List<GrantedHeroLoot>();
        foreach (ActivityParticipant recipient in recipients)
        {
            heroLoots.Add(
                await GrantToHeroAsync(request, table, source, recipient.HeroId, cancellationToken)
            );
        }

        return new GrantEnemyLootResult(request.EnemyId, heroLoots);
    }

    private async Task<GrantedHeroLoot> GrantToHeroAsync(
        GrantEnemyLootCommand request,
        LootTable table,
        RewardSource source,
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        string rewardKey = RewardEntity.CreateKey(request.EnemyType, request.EnemyId, heroId);

        // A replayed command returns the persisted loot instead of drawing a new one.
        RewardEntity? existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request, heroId);
        }

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(heroId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory for hero '{heroId}' was not found.");

        // A concurrent delivery of the same operation may have been granted while this one
        // was waiting for the inventory lock.
        existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request, heroId);
        }

        DateTimeOffset now = clock.UtcNow;
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

        // Every selected item comes from the enemy's loot table, by construction.
        foreach (SelectedLootItem selected in lootSelectionService.Select(table))
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
