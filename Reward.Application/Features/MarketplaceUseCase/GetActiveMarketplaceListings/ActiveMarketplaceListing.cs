namespace Reward.Application.Features.MarketplaceUseCase.GetActiveMarketplaceListings;

public sealed record ActiveMarketplaceListing(
    Guid Id,
    Guid ItemInstanceId,
    string ItemName,
    string Category,
    string Rarity,
    int Quantity,
    decimal Price,
    Guid SellerId
);
