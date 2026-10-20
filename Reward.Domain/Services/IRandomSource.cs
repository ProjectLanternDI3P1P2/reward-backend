namespace Reward.Domain.Services;

public interface IRandomSource
{
    /// <summary>Returns a uniformly distributed integer in [0, <paramref name="maxExclusive"/>).</summary>
    int NextInt32(int maxExclusive);
}
