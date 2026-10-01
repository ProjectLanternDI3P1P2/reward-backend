namespace Reward.Domain.Enums;

/// <summary>State of a registered participant when a group activity ends.</summary>
public enum ParticipationStatus
{
    Active,
    Dead,
    Disconnected,
    Left,
}
