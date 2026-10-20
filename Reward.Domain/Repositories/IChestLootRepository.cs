using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

/// <summary>Provides the persistence operations needed to generate chest loot once.</summary>
public interface IChestLootRepository
{
    /// <summary>Serializes generators targeting the same run and chest for the current transaction.</summary>
    Task AcquireGenerationLockAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    );

    /// <summary>Gets the immutable generation already stored for a run and chest.</summary>
    Task<ChestLootGeneration?> GetGenerationAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    );

    /// <summary>Gets the exact CHEST table for a floor and normalized difficulty.</summary>
    Task<LootTable?> GetLootTableAsync(
        int floor,
        string difficulty,
        CancellationToken cancellationToken
    );

    /// <summary>Gets a configured reward source by its stable code.</summary>
    Task<RewardSource?> GetRewardSourceAsync(string name, CancellationToken cancellationToken);

    /// <summary>Adds a new immutable chest generation graph.</summary>
    void AddGeneration(ChestLootGeneration generation);

    /// <summary>Adds a message to the transactional outbox.</summary>
    void AddOutboxMessage(OutboxMessage message);
}
