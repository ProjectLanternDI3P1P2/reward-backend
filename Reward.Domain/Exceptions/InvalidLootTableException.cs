namespace Reward.Domain.Exceptions;

/// <summary>Indicates that a configured loot table cannot produce valid loot.</summary>
public sealed class InvalidLootTableException(string message) : Exception(message);
