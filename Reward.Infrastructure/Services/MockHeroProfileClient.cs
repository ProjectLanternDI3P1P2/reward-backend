using Reward.Application.Ports;

namespace Reward.Infrastructure.Services;

/// <summary>Temporary local source until Player publishes the hero profile contract.</summary>
public sealed class MockHeroProfileClient : IHeroProfileClient
{
    private static readonly IReadOnlyDictionary<Guid, HeroProfile> Heroes = new Dictionary<
        Guid,
        HeroProfile
    >
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            3,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WARRIOR" }
        ),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            3,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MAGE" }
        ),
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ROGUE" }
        ),
    };

    public Task<HeroProfile> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            Heroes.GetValueOrDefault(heroId)
                ?? throw new KeyNotFoundException($"Hero '{heroId}' was not found.")
        );
    }
}
