using MediatR;
using Reward.Application.Messaging;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.RewardUseCase.GenerateChestLoot;

/// <summary>Generates and persists chest loot once, or replays the persisted reward.</summary>
public sealed class GenerateChestLootCommandHandler(
    IChestLootRepository repository,
    LootSelectionService selector,
    IClock clock
) : IRequestHandler<GenerateChestLootCommand, GenerateChestLootResult>
{
    private const string ChestSource = "CHEST";

    /// <summary>Handles one idempotent chest generation command.</summary>
    public async Task<GenerateChestLootResult> Handle(
        GenerateChestLootCommand request,
        CancellationToken cancellationToken
    )
    {
        // Serialize identical run/chest requests before any read or random draw.
        await repository.AcquireGenerationLockAsync(
            request.DungeonRunId,
            request.ChestId,
            cancellationToken
        );

        // Replay the stored graph without consulting current tables or consuming randomness.
        ChestLootGeneration? existing = await repository.GetGenerationAsync(
            request.DungeonRunId,
            request.ChestId,
            cancellationToken
        );
        if (existing is not null)
        {
            GenerateChestLootResult replay = Map(existing, alreadyGenerated: true);
            repository.AddOutboxMessage(
                ChestLootOutboxMessages.CreateGenerationLog(
                    request.CommandId,
                    replay,
                    "REPLAYED",
                    clock.UtcNow
                )
            );
            return replay;
        }

        // Require an exact non-legacy CHEST table for the normalized context.
        LootTable table =
            await repository.GetLootTableAsync(request.Floor, request.Difficulty, cancellationToken)
            ?? throw new LootTableNotFoundException(
                $"CHEST loot table for floor {request.Floor} and difficulty '{request.Difficulty}' was not found."
            );

        // Resolve the stable reward source before constructing the persisted aggregate.
        RewardSource source =
            await repository.GetRewardSourceAsync(ChestSource, cancellationToken)
            ?? throw new InvalidLootTableException("CHEST reward source is not configured.");

        // Perform both weighted stages only after the advisory lock and exact lookup succeed.
        IReadOnlyList<SelectedLootItem> selectedItems = selector.Select(table);
        DateTimeOffset now = clock.UtcNow;

        // Build the reward and its aggregated item rows as one tracked graph.
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.Empty,
            RunId = request.DungeonRunId,
            RewardSourceId = source.Id,
            RewardSource = source,
            Type = RewardType.Item,
            Status = RewardStatus.Applied,
            RewardKey = $"chest:{request.DungeonRunId:N}:{request.ChestId:N}",
            CreatedAt = now,
        };
        foreach (SelectedLootItem selectedItem in selectedItems)
        {
            reward.Items.Add(
                new RewardItem
                {
                    Id = Guid.NewGuid(),
                    RewardId = reward.Id,
                    Reward = reward,
                    ItemId = selectedItem.Item.Id,
                    Item = selectedItem.Item,
                    ItemNameSnapshot = selectedItem.Item.Name,
                    ItemRaritySnapshot = selectedItem.Item.Rarity.Label,
                    Quantity = selectedItem.Quantity,
                    CreatedAt = now,
                }
            );
        }

        // Persist the chosen table and context beside the reward for immutable replay and audit.
        var generation = new ChestLootGeneration
        {
            Id = Guid.NewGuid(),
            CommandId = request.CommandId,
            DungeonRunId = request.DungeonRunId,
            ChestId = request.ChestId,
            LootTableId = table.Id,
            LootTable = table,
            Floor = request.Floor,
            Difficulty = request.Difficulty,
            RewardId = reward.Id,
            Reward = reward,
            CreatedAt = now,
        };
        repository.AddGeneration(generation);

        // Enqueue the single business event and GENERATED log in the same database transaction.
        GenerateChestLootResult result = Map(generation, alreadyGenerated: false);
        repository.AddOutboxMessage(
            ChestLootOutboxMessages.CreateGeneratedEvent(request.CommandId, result, now)
        );
        repository.AddOutboxMessage(
            ChestLootOutboxMessages.CreateGenerationLog(request.CommandId, result, "GENERATED", now)
        );
        return result;
    }

    private static GenerateChestLootResult Map(
        ChestLootGeneration generation,
        bool alreadyGenerated
    )
    {
        // Project only persisted values so replay never depends on mutable table configuration.
        return new GenerateChestLootResult(
            generation.RewardId,
            generation.DungeonRunId,
            generation.ChestId,
            generation.LootTableId,
            generation.Floor,
            generation.Difficulty,
            alreadyGenerated,
            generation
                .Reward.Items.OrderBy(item => item.ItemId)
                .Select(item => new GeneratedChestLootItem(
                    item.ItemId,
                    item.ItemNameSnapshot,
                    item.ItemRaritySnapshot,
                    item.Quantity
                ))
                .ToList()
        );
    }
}
