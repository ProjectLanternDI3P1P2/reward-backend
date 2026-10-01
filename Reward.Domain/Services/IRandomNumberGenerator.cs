namespace Reward.Domain.Services;

/// <summary>Supplies bounded random values to domain selection rules.</summary>
public interface IRandomNumberGenerator
{
    /// <summary>Returns a value from zero inclusive to the supplied bound exclusive.</summary>
    long NextInt64(long exclusiveMaximum);
}
