using Reward.Application.Abstractions;
using MediatR;
using Reward.Infrastructure.Persistence;

namespace Reward.Infrastructure.PipelineBehavior;

public sealed class CommandTransactionBehavior<TRequest, TResponse>(RewardDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        TResponse response = await next(cancellationToken);

        if (request is ICommand)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
