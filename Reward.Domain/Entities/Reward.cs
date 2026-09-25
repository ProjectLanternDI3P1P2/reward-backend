namespace Reward.Domain.Entities;

public sealed class Reward
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid RewardSourceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RewardKey { get; set; } = string.Empty;
    public int XpAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public RewardSource RewardSource { get; set; } = null!;
}
