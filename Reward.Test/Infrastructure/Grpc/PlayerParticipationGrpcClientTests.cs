using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Moq;
using Reward.Contracts.V1;
using Reward.Domain.Exceptions;
using Reward.Infrastructure.Grpc.Clients;
using Reward.Infrastructure.Grpc.Configuration;

namespace Reward.Test.Infrastructure.Grpc;

public sealed class PlayerParticipationGrpcClientTests
{
    private readonly Guid runId = Guid.NewGuid();
    private readonly Mock<RewardPlayerService.RewardPlayerServiceClient> grpcClient = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsRunParticipantAsync_PlayerAnswers_ReturnsItsAnswerForTheGivenUserAndRun(
        bool isParticipant
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
            .Returns(Reply(new GetRunParticipationReply { IsParticipant = isParticipant }));

        // Act
        bool result = await CreateClient()
            .IsRunParticipantAsync("keycloak-user", runId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(isParticipant);
        sent!.ExternalUserId.Should().Be("keycloak-user");
        sent.DungeonRunId.Should().Be(runId.ToString());
        callOptions.Deadline.Should().NotBeNull().And.BeAfter(DateTime.UtcNow);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Unimplemented)]
    public async Task IsRunParticipantAsync_PlayerCannotAnswer_ThrowsDependencyUnavailable(
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
                .IsRunParticipantAsync(
                    "keycloak-user",
                    runId,
                    TestContext.Current.CancellationToken
                );

        // Assert: participation is never assumed when Player cannot confirm it.
        await act.Should().ThrowAsync<DependencyUnavailableException>();
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
