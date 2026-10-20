namespace Reward.Domain.Entities;

public sealed class ItemModifier
{
    public Guid ItemId { get; set; }
    public Guid ModifierId { get; set; }
    public Item Item { get; set; } = null!;
    public Modifier Modifier { get; set; } = null!;
}
