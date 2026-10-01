using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Application.Features.MarketplaceUseCase.GetActiveMarketplaceListings;

public sealed class GetActiveMarketplaceListingsQueryHandler(IMarketplaceRepository repository)
    : IRequestHandler<GetActiveMarketplaceListingsQuery, IReadOnlyList<ActiveMarketplaceListing>>
{
    public async Task<IReadOnlyList<ActiveMarketplaceListing>> Handle(
        GetActiveMarketplaceListingsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<MarketplaceListing> listings = await repository.GetActiveListingsAsync(
            cancellationToken
        );

        return listings
            .Select(listing => new ActiveMarketplaceListing(
                listing.Id,
                listing.ItemInstanceId,
                listing.ItemInstance.Item.Name,
                listing.ItemInstance.Item.Category.Label,
                listing.ItemInstance.Item.Rarity.Label,
                listing.Quantity,
                listing.Price,
                listing.SellerId
            ))
            .ToList();
    }
}
