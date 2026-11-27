using Grpc.Core;
using Microsoft.Extensions.Options;
using Reward.Application.Exceptions;
using Reward.Application.Ports;
using Reward.Contracts.V1;
using Reward.Infrastructure.Grpc.Configuration;

namespace Reward.Infrastructure.Grpc.Clients;

/// <summary>gRPC implementation of the participation port, served by the Player service.</summary>
public sealed class PlayerParticipationGrpcClient(
    RewardPlayerService.RewardPlayerServiceClient client,
    IOptions<PlayerGrpcClientOptions> options
) : IPlayerParticipationClient
{
    public async Task<bool> IsRunParticipantAsync(
        string externalUserId,
        Guid dungeonRunId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            GetRunParticipationReply reply = await client.GetRunParticipationAsync(
                new GetRunParticipationRequest
                {
                    ExternalUserId = externalUserId,
                    DungeonRunId = dungeonRunId.ToString(),
                },
                new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds),
                    cancellationToken: cancellationToken
                )
            );

            return reply.IsParticipant;
        }
        // Without Player's answer nothing proves the caller took part: the access is refused
        // as unavailable, never granted by default.
        catch (RpcException exception) when (exception.StatusCode != StatusCode.Cancelled)
        {
            throw new ExternalServiceUnavailableException("Player", exception);
        }
    }
}
