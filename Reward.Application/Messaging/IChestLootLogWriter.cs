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

/// <summary>Persists rejected chest request logs outside the rolled-back command transaction.</summary>
public interface IChestLootLogWriter
{
    /// <summary>Writes a REJECTED structured log without changing reward state.</summary>
    Task WriteRejectedAsync(RejectedChestLootLog log, CancellationToken cancellationToken);
}
