using Reward.Domain.Enums;

namespace Reward.Domain.ValueObjects;

/// <summary>A player registered in the roster of a group activity.</summary>
public sealed record ActivityParticipant(Guid PlayerId, ParticipationStatus Status);
