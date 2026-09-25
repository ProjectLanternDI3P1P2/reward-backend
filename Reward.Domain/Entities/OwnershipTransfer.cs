namespace Reward.Domain.Entities;

public sealed class OwnershipTransfer
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public Guid FromOwnerId { get; set; }
    public Guid ToOwnerId { get; set; }
    public Guid FromInventoryId { get; set; }
    public Guid ToInventoryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public MarketplaceTransaction Transaction { get; set; } = null!;
    public ItemInstance ItemInstance { get; set; } = null!;
    public Inventory FromInventory { get; set; } = null!;
    public Inventory ToInventory { get; set; } = null!;
}
