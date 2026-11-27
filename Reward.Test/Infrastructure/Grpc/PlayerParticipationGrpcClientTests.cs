using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Moq;
using Reward.Application.Exceptions;
using Reward.Application.Ports;
using Reward.Contracts.V1;
using Reward.Infrastructure.Grpc.Clients;
using Reward.Infrastructure.Grpc.Configuration;

namespace Reward.Test.Infrastructure.Grpc;

public sealed class PlayerParticipationGrpcClientTests
{
    private const string PlayerId = "980ec1ab-c080-4bfb-9ab5-9baea1305588";
    private readonly Guid runId = Guid.NewGuid();
    private readonly Mock<RewardPlayerService.RewardPlayerServiceClient> grpcClient = new();

    [Theory]
    [InlineData(true, PlayerId, PlayerId)]
    [InlineData(false, "", null)]
    [InlineData(false, PlayerId, null)]
    [InlineData(true, "", null)]
    [InlineData(true, "invalid-id", null)]
    [InlineData(true, "00000000-0000-0000-0000-000000000000", null)]
    public async Task GetRunParticipationAsync_PlayerAnswers_ReturnsParticipationAndKnownPlayerId(
        bool isParticipant,
        string replyPlayerId,
        string? expectedPlayerId
    )
    {
        // Arrange
        GetRunParticipationRequest? sent = null;
        CallOptions callOptions = default;
        grpcClient
            .Setup(client =>
                client.GetRunParticipationAsync(
                    It.IsAny<GetRunParticipationRequest>(),
                    It.IsAny<CallOptions>()
                )
            )
            .Callback<GetRunParticipationRequest, CallOptions>(
                (request, options) =>
                {
                    sent = request;
                    callOptions = options;
                }
            )
            .Returns(
                Reply(
                    new GetRunParticipationReply
                    {
                        IsParticipant = isParticipant,
                        PlayerId = replyPlayerId,
                    }
                )
            );

        // Act
        RunParticipation result = await CreateClient()
            .GetRunParticipationAsync(
                "keycloak-user",
                runId,
                TestContext.Current.CancellationToken
            );

        // Assert
        result.IsParticipant.Should().Be(isParticipant);
        result.PlayerId.Should().Be(expectedPlayerId is null ? null : Guid.Parse(expectedPlayerId));
        sent!.ExternalUserId.Should().Be("keycloak-user");
        sent.DungeonRunId.Should().Be(runId.ToString());
        callOptions.Deadline.Should().NotBeNull().And.BeAfter(DateTime.UtcNow);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Unimplemented)]
    public async Task GetRunParticipationAsync_PlayerCannotAnswer_ThrowsDependencyUnavailable(
        StatusCode statusCode
    )
    {
        // Arrange: Unimplemented is what Reward gets until Player ships RewardPlayerService.
        grpcClient
            .Setup(client =>
                client.GetRunParticipationAsync(
                    It.IsAny<GetRunParticipationRequest>(),
                    It.IsAny<CallOptions>()
                )
            )
            .Throws(new RpcException(new Status(statusCode, "Player failed.")));

        // Act
        Func<Task> act = () =>
            CreateClient()
                .GetRunParticipationAsync(
                    "keycloak-user",
                    runId,
                    TestContext.Current.CancellationToken
                );

        // Assert: participation is never assumed when Player cannot confirm it.
        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    private PlayerParticipationGrpcClient CreateClient() =>
        new(grpcClient.Object, Options.Create(new PlayerGrpcClientOptions { TimeoutSeconds = 5 }));

    private static AsyncUnaryCall<T> Reply<T>(T response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { }
        );
}
