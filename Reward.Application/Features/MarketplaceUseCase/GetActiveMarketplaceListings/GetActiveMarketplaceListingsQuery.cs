using MediatR;

namespace Reward.Application.Features.MarketplaceUseCase.GetActiveMarketplaceListings;

public sealed record GetActiveMarketplaceListingsQuery
    : IRequest<IReadOnlyList<ActiveMarketplaceListing>>;
