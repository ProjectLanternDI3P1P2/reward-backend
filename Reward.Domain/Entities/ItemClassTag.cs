namespace Reward.Domain.Entities;

public sealed class ItemClassTag
{
    public Guid ItemId { get; set; }
    public Guid ClassTagId { get; set; }
    public Item Item { get; set; } = null!;
    public ClassTag ClassTag { get; set; } = null!;
}
