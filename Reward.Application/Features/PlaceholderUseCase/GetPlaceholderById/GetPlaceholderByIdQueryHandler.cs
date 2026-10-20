using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Application.Features.PlaceholderUseCase.GetPlaceholderById;

public sealed class GetPlaceholderByIdQueryHandler(IPlaceholderRepository repository)
    : IRequestHandler<GetPlaceholderByIdQuery, Placeholder?>
{
    public Task<Placeholder?> Handle(
        GetPlaceholderByIdQuery request,
        CancellationToken cancellationToken
    ) => repository.GetByIdAsync(request.PlaceholderId, cancellationToken);
}
