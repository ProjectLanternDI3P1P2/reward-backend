namespace Reward.Domain.Exceptions;

public sealed class InventoryCapacityExceededException(string capacityType, int capacity)
    : InvalidOperationException(
        $"The inventory has reached its {capacityType} capacity of {capacity}."
    );
