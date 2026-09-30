using Reward.Domain.Enums;

namespace Reward.Domain.Entities;

public sealed class Reward
{
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public Guid RunId { get; set; }
    public Guid RewardSourceId { get; set; }
    public RewardType Type { get; set; }
    public RewardStatus Status { get; set; }
    public string RewardKey { get; set; } = string.Empty;
    public int XpAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public RewardSource RewardSource { get; set; } = null!;
    public ICollection<RewardItem> Items { get; set; } = new List<RewardItem>();

    /// <summary>
    /// Identifies one reward operation: the event that caused it and the hero receiving it.
    /// Replaying the same operation always yields the same key.
    /// </summary>
    public static string CreateKey(RewardSourceType sourceType, Guid causeId, Guid heroId) =>
        $"{sourceType.ToCode()}:{causeId}:{heroId}";
}
