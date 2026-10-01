using System.Security.Cryptography;
using Reward.Domain.Services;

namespace Reward.Infrastructure.Services;

/// <summary>
/// Unbiased and thread-safe; unpredictable so a client cannot anticipate who wins a unique reward.
/// </summary>
public sealed class CryptoRandomSource : IRandomSource
{
    public int NextInt32(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
}
