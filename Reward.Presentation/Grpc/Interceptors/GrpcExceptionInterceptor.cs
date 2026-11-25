using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Exceptions;
using ILogger = Serilog.ILogger;

namespace Reward.Presentation.Grpc.Interceptors;

/// <summary>Maps application exceptions to standard gRPC statuses for every gRPC endpoint.</summary>
public sealed class GrpcExceptionInterceptor(ILogger logger, IHostEnvironment environment)
    : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (KeyNotFoundException exception)
        {
            logger.Warning(exception, "gRPC resource not found.");
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (LootTableNotFoundException exception)
        {
            // Expose the missing exact dungeon context as a stable caller-visible status.
            logger.Warning(exception, "gRPC loot table not found.");
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (InvalidLootTableException exception)
        {
            // Distinguish invalid server loot configuration from malformed caller input.
            logger.Error(exception, "gRPC loot table configuration is invalid.");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "gRPC validation error.");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Validation error."));
        }
        catch (DomainException exception)
        {
            // A full inventory or a replay with a different content is a business refusal,
            // not a server failure.
            logger.Warning(exception, "Business rule refused the gRPC operation.");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.Warning(exception, "Concurrent update detected during a gRPC call.");
            throw new RpcException(
                new Status(
                    StatusCode.Aborted,
                    "The resource was modified by another request. Retry the call."
                )
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Unhandled gRPC exception.");
            string detail = environment.IsDevelopment()
                ? exception.Message
                : "Internal server error.";
            throw new RpcException(new Status(StatusCode.Internal, detail));
        }
    }
}
