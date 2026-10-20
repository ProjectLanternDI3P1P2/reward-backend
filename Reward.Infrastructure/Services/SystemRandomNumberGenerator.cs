using Reward.Domain.Services;

namespace Reward.Infrastructure.Services;

/// <summary>Supplies thread-safe runtime randomness for loot selection.</summary>
public sealed class SystemRandomNumberGenerator : IRandomNumberGenerator
{
    public long NextInt64(long exclusiveMaximum)
    {
        // Fail clearly if corrupt weights ever produce a non-positive total.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);

        // Random.Shared is thread-safe and adequate for non-cryptographic game loot.
        return Random.Shared.NextInt64(exclusiveMaximum);
    }
}
