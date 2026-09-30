namespace Reward.Domain.Exceptions;

/// <summary>Indicates that no exact loot table exists for the requested dungeon context.</summary>
public sealed class LootTableNotFoundException(string message) : Exception(message);
