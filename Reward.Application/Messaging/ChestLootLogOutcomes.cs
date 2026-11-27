namespace Reward.Application.Messaging;

/// <summary>
/// The <c>outcome</c> values of the chest structured logs published in
/// <c>reward_events_v1.proto</c>. Consumers match on these exact strings, so they are part of
/// the event contract and must not change.
/// </summary>
public static class ChestLootLogOutcomes
{
    /// <summary>The loot of a chest was drawn and persisted for the first time.</summary>
    public const string Generated = "GENERATED";

    /// <summary>A repeated generation request was answered from the persisted loot.</summary>
    public const string Replayed = "REPLAYED";

    /// <summary>A generation request was refused before any loot was drawn.</summary>
    public const string Rejected = "REJECTED";

    /// <summary>The whole chest content was transferred into the hero inventory.</summary>
    public const string Claimed = "CLAIMED";

    /// <summary>The chest had already been claimed: nothing was transferred.</summary>
    public const string AlreadyEmpty = "ALREADY_EMPTY";

    /// <summary>The inventory could not hold the whole chest: nothing was transferred.</summary>
    public const string CapacityRefused = "CAPACITY_REFUSED";
}
