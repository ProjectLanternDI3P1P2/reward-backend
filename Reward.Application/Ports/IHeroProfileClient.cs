namespace Reward.Application.Ports;

/// <summary>Read-only view of the hero data required to validate equipment eligibility.</summary>
public interface IHeroProfileClient
{
    Task<HeroProfile> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken);
}

public sealed record HeroProfile(Guid HeroId, int Level, IReadOnlySet<string> ClassTags);
