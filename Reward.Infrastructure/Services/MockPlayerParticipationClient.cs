using Microsoft.Extensions.Logging;
using Reward.Application.Ports;

namespace Reward.Infrastructure.Services;

/// <summary>
/// Local stand-in for Player's RewardPlayerService while it is not implemented: every
/// authenticated user is treated as a participant. Enabled by Grpc:Player:UseDevelopmentMock.
/// </summary>
public sealed class MockPlayerParticipationClient(ILogger<MockPlayerParticipationClient> logger)
    : IPlayerParticipationClient
{
    public Task<bool> IsRunParticipantAsync(
        string externalUserId,
        Guid dungeonRunId,
        CancellationToken cancellationToken
    )
    {
        logger.LogWarning(
            "Development participation mock accepted user {UserId} for run {DungeonRunId}",
            externalUserId,
            dungeonRunId
        );
        return Task.FromResult(true);
    }
}
