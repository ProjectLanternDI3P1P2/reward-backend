namespace Reward.Domain.Entities;

public sealed class RunInventorySession
{
    public Guid Id { get; set; }
    public Guid InventorySnapshotId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public InventorySnapshot InventorySnapshot { get; set; } = null!;
}
