namespace Reward.Domain.Exceptions;

/// <summary>The inventory has no free slot left for the received item.</summary>
public sealed class InventoryCapacityExceededException(string capacityType, int capacity)
    : DomainException($"The inventory has reached its {capacityType} capacity of {capacity}.");
