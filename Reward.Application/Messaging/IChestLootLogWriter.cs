namespace Reward.Application.Messaging;

/// <summary>Describes a rejected chest request before typed identifiers are guaranteed.</summary>
public sealed record RejectedChestLootLog(
    string CommandId,
    string DungeonRunId,
    string ChestId,
    int Floor,
    string Difficulty,
    string Reason
);

/// <summary>Identifies a claim whose complete loot could not fit in the inventory.</summary>
public sealed record RefusedChestLootClaimLog(Guid HeroId, Guid DungeonRunId, Guid ChestId);

/// <summary>Persists rejected chest request logs outside the rolled-back command transaction.</summary>
public interface IChestLootLogWriter
{
    /// <summary>Writes a REJECTED structured log without changing reward state.</summary>
    Task WriteRejectedAsync(RejectedChestLootLog log, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to persist a CAPACITY_REFUSED log after the claim transaction rolled back.
    /// Logging failures are reported internally and never replace the capacity response.
    /// </summary>
    Task TryWriteCapacityRefusedAsync(
        RefusedChestLootClaimLog log,
        CancellationToken cancellationToken
    );
}
