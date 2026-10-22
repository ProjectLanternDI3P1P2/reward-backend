using FluentAssertions;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence.Seeding;

namespace Reward.Test.Domain.Enums;

public sealed class ItemRarityExtensionsTests
{
    [Fact]
    public void ToRank_EveryDefaultRarity_MatchesTheRankOfTheSeededCatalogue()
    {
        // Arrange the rarities the game catalogue is seeded with.
        IReadOnlyList<Rarity> catalogue = RewardFakeDataGenerator.CreateRarities(
            DateTimeOffset.UtcNow
        );

        // Act
        Dictionary<string, int> ranks = Enum.GetValues<ItemRarity>()
            .ToDictionary(rarity => rarity.ToString(), rarity => rarity.ToRank());

        // Assert a rank change in the catalogue must change the domain mapping with it.
        catalogue
            .ToDictionary(rarity => rarity.Label, rarity => rarity.Rank)
            .Should()
            .BeEquivalentTo(ranks);
    }

    [Fact]
    public void ToRank_UnknownRarity_Throws()
    {
        // Act
        Func<int> action = () => ((ItemRarity)99).ToRank();

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
