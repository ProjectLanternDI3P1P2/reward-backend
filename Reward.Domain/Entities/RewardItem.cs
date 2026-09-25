namespace Reward.Domain.Entities;

public sealed class RewardItem
{
    public Guid Id { get; set; }
    public Guid RewardId { get; set; }
    public Guid ItemId { get; set; }
    public Guid? ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Reward Reward { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public ItemInstance? ItemInstance { get; set; }
}
