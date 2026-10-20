using Reward.Application.Abstractions;
using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;

namespace Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;

/// <summary>
/// Awards a unique reward of a completed group activity to one participant drawn at random
/// among the eligible ones.
/// </summary>
public sealed record AwardUniqueGroupRewardCommand(
    Guid RunId,
    RewardSourceType SourceType,
    Guid CauseId,
    Guid ItemId,
    int Quantity,
    IReadOnlyList<ActivityParticipant> Participants
) : ICommand<AwardUniqueGroupRewardResult>;
