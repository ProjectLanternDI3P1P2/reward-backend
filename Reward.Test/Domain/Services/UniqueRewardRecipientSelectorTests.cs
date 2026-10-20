using FluentAssertions;
using Reward.Domain.Enums;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;

namespace Reward.Test.Domain.Services;

public sealed class UniqueRewardRecipientSelectorTests
{
    [Fact]
    public void SelectRecipient_SingleEligiblePlayer_SelectsThatPlayer()
    {
        // Arrange
        ActivityParticipant eligible = CreateParticipant(ParticipationStatus.Dead);
        ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);

        // Act
        ActivityParticipant recipient = UniqueRewardRecipientSelector.SelectRecipient(
            [left, eligible],
            new FixedRandomSource(0)
        );

        // Assert
        recipient.Should().Be(eligible);
    }

    [Fact]
    public void SelectRecipient_MultipleEligiblePlayers_DrawsAmongEligibleOnly()
    {
        // Arrange
        ActivityParticipant first = CreateParticipant(ParticipationStatus.Active);
        ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);
        ActivityParticipant second = CreateParticipant(ParticipationStatus.Disconnected);
        var random = new FixedRandomSource(1);

        // Act
        ActivityParticipant recipient = UniqueRewardRecipientSelector.SelectRecipient(
            [first, left, second],
            random
        );

        // Assert
        recipient.Should().Be(second);
        random.RequestedMaxExclusive.Should().Be(2);
    }

    [Fact]
    public void SelectRecipient_ManyDraws_GivesEachEligiblePlayerTheSameChance()
    {
        // Arrange
        ActivityParticipant[] eligible =
        [
            CreateParticipant(ParticipationStatus.Active),
            CreateParticipant(ParticipationStatus.Dead),
            CreateParticipant(ParticipationStatus.Disconnected),
            CreateParticipant(ParticipationStatus.Active),
        ];
        ActivityParticipant[] participants =
        [
            .. eligible,
            CreateParticipant(ParticipationStatus.Left),
        ];
        // Seeded so the test is deterministic.
        var random = new SeededRandomSource(seed: 2026);
        const int draws = 40_000;

        // Act
        Dictionary<Guid, int> wins = Enumerable
            .Range(0, draws)
            .Select(_ => UniqueRewardRecipientSelector.SelectRecipient(participants, random))
            .GroupBy(recipient => recipient.PlayerId)
            .ToDictionary(group => group.Key, group => group.Count());

        // Assert
        wins.Keys.Should().BeEquivalentTo(eligible.Select(participant => participant.PlayerId));
        int expectedWins = draws / eligible.Length;
        wins.Values.Should()
            .OnlyContain(count => Math.Abs(count - expectedWins) < expectedWins / 20);
    }

    [Fact]
    public void SelectRecipient_NoEligiblePlayer_ThrowsInvalidOperationException()
    {
        // Arrange
        ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);

        // Act
        Action act = () =>
            UniqueRewardRecipientSelector.SelectRecipient([left], new FixedRandomSource(0));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static ActivityParticipant CreateParticipant(ParticipationStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), status);

    private sealed class FixedRandomSource(int value) : IRandomSource
    {
        public int? RequestedMaxExclusive { get; private set; }

        public int NextInt32(int maxExclusive)
        {
            RequestedMaxExclusive = maxExclusive;
            return value;
        }
    }

    private sealed class SeededRandomSource(int seed) : IRandomSource
    {
        private readonly Random random = new(seed);

        public int NextInt32(int maxExclusive) => random.Next(maxExclusive);
    }
}
