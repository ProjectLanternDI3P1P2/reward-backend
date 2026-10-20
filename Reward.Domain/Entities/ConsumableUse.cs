namespace Reward.Domain.Entities;

public sealed class ConsumableUse
{
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public Guid? ItemInstanceId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ItemInstance? ItemInstance { get; set; }
}
