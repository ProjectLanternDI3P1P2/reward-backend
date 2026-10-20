using Reward.Domain.Enums;

namespace Reward.Domain.ValueObjects;

/// <summary>
/// A player registered in the roster of a group activity, with the hero they play.
/// Rewards are credited to the hero's inventory.
/// </summary>
public sealed record ActivityParticipant(Guid PlayerId, Guid HeroId, ParticipationStatus Status);
