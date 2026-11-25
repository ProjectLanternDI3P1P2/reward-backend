namespace Reward.Presentation.Middleware;

/// <summary>
/// Stable machine-readable codes exposed in the <c>code</c> extension of ProblemDetails
/// responses, so clients can react to a specific refusal without parsing its message.
/// </summary>
public static class ProblemErrorCodes
{
    /// <summary>The inventory cannot hold every item of the operation (HTTP 409).</summary>
    public const string InventoryCapacityInsufficient = "INVENTORY_CAPACITY_INSUFFICIENT";
}
