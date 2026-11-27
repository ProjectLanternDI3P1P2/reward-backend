using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;
using Reward.Domain.Exceptions;
using Reward.Presentation.Grpc.Interceptors;
using Reward.Test.TestSupport;
using Serilog.Core;

namespace Reward.Test.Presentation.Grpc;

public class GrpcExceptionInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_DomainException_ThrowsFailedPrecondition()
    {
        // Act
        RpcException rpcException = await InterceptAsync(
            new RewardConflictException("MONSTER:1:2", "was already granted for another run")
        );

        // Assert
        rpcException.StatusCode.Should().Be(StatusCode.FailedPrecondition);
        rpcException.Status.Detail.Should().Contain("another run");
    }

    [Fact]
    public async Task UnaryServerHandler_InvalidOperationException_ThrowsInternal()
    {
        // Act: a configuration failure must not look like a refusal the caller can resolve.
        RpcException rpcException = await InterceptAsync(
            new InvalidOperationException("The reward source 'BOSS' is not configured.")
        );

        // Assert
        rpcException.StatusCode.Should().Be(StatusCode.Internal);
    }

    [Fact]
    public async Task UnaryServerHandler_ConcurrentUpdate_ThrowsAborted()
    {
        // Act
        RpcException rpcException = await InterceptAsync(
            new DbUpdateConcurrencyException("Concurrency token mismatch.")
        );

        // Assert
        rpcException.StatusCode.Should().Be(StatusCode.Aborted);
    }

    [Fact]
    public async Task UnaryServerHandler_Cancellation_IsNotTurnedIntoAnInternalError()
    {
        // Act
        Func<Task> act = () => Intercept(new OperationCanceledException());

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<RpcException> InterceptAsync(Exception exception)
    {
        Func<Task> act = () => Intercept(exception);

        return (await act.Should().ThrowAsync<RpcException>()).Which;
    }

    // Runs the interceptor around a gRPC handler that fails with the given exception.
    private static Task<object> Intercept(Exception exception)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Development);
        var interceptor = new GrpcExceptionInterceptor(Logger.None, environment.Object);

        return interceptor.UnaryServerHandler<object, object>(
            new object(),
            new FakeServerCallContext(TestContext.Current.CancellationToken),
            (_, _) => throw exception
        );
    }
}
