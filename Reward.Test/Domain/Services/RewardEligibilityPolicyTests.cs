using FluentAssertions;
using Reward.Domain.Enums;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;

namespace Reward.Test.Domain.Services;

public sealed class RewardEligibilityPolicyTests
{
    [Fact]
    public void GetEligiblePlayerIds_ActiveParticipants_ReturnsAllOfThem()
    {
        // Arrange
        var first = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Active
        );
        var second = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Active
        );

        // Act
        IReadOnlyList<Guid> eligiblePlayerIds = RewardEligibilityPolicy.GetEligiblePlayerIds([
            first,
            second,
        ]);

        // Assert
        eligiblePlayerIds.Should().BeEquivalentTo([first.PlayerId, second.PlayerId]);
    }

    [Fact]
    public void IsEligible_PlayerNotRegisteredInActivity_ReturnsFalse()
    {
        // Arrange
        var participant = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Active
        );

        // Act
        bool isEligible = RewardEligibilityPolicy.IsEligible(Guid.NewGuid(), [participant]);

        // Assert
        isEligible.Should().BeFalse();
    }

    [Fact]
    public void IsEligible_ParticipantDiedBeforeActivityEnded_ReturnsTrue()
    {
        // Arrange
        var participant = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Dead
        );

        // Act
        bool isEligible = RewardEligibilityPolicy.IsEligible(participant.PlayerId, [participant]);

        // Assert
        isEligible.Should().BeTrue();
    }

    [Fact]
    public void IsEligible_ParticipantTemporarilyDisconnected_ReturnsTrue()
    {
        // Arrange
        var participant = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Disconnected
        );

        // Act
        bool isEligible = RewardEligibilityPolicy.IsEligible(participant.PlayerId, [participant]);

        // Assert
        isEligible.Should().BeTrue();
    }

    [Fact]
    public void IsEligible_ParticipantLeftVoluntarily_ReturnsFalse()
    {
        // Arrange
        var participant = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Left
        );

        // Act
        bool isEligible = RewardEligibilityPolicy.IsEligible(participant.PlayerId, [participant]);

        // Assert
        isEligible.Should().BeFalse();
    }

    [Fact]
    public void GetEligiblePlayerIds_MixedParticipants_ExcludesOnlyPlayersWhoLeft()
    {
        // Arrange
        var active = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Active
        );
        var dead = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Dead
        );
        var disconnected = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Disconnected
        );
        var left = new ActivityParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationStatus.Left
        );

        // Act
        IReadOnlyList<Guid> eligiblePlayerIds = RewardEligibilityPolicy.GetEligiblePlayerIds([
            active,
            dead,
            disconnected,
            left,
        ]);

        // Assert
        eligiblePlayerIds
            .Should()
            .BeEquivalentTo([active.PlayerId, dead.PlayerId, disconnected.PlayerId]);
    }

    [Fact]
    public void GetEligiblePlayerIds_PlayerRegisteredTwice_ThrowsArgumentException()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();

        // Act
        Action act = () =>
            RewardEligibilityPolicy.GetEligiblePlayerIds([
                new ActivityParticipant(playerId, Guid.NewGuid(), ParticipationStatus.Active),
                new ActivityParticipant(playerId, Guid.NewGuid(), ParticipationStatus.Left),
            ]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
