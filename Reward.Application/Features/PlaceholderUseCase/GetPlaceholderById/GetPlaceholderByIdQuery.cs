using MediatR;
using Reward.Domain.Entities;

namespace Reward.Application.Features.PlaceholderUseCase.GetPlaceholderById;

public sealed record GetPlaceholderByIdQuery(Guid PlaceholderId) : IRequest<Placeholder?>;
