using FluentAssertions;
using Moq;
using Reward.Application.Features.MarketplaceUseCase.GetActiveMarketplaceListings;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Test.Features.MarketplaceUseCase;

public sealed class GetActiveMarketplaceListingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ActiveListingsExist_MapsItemAndSellerDetails()
    {
        // Arrange
        Guid listingId = Guid.NewGuid();
        Guid itemInstanceId = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();
        var listing = new MarketplaceListing
        {
            Id = listingId,
            ItemInstanceId = itemInstanceId,
            SellerId = sellerId,
            Quantity = 2,
            Price = 42.5m,
            Status = MarketplaceListingStatus.Active,
            ItemInstance = new ItemInstance
            {
                Id = itemInstanceId,
                Item = new Item
                {
                    Name = "Obsidian Blade",
                    Category = new Category { Label = "WEAPON" },
                    Rarity = new Rarity { Label = "Epic" },
                },
            },
        };
        var repository = new Mock<IMarketplaceRepository>();
        repository
            .Setup(repository => repository.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing]);
        var handler = new GetActiveMarketplaceListingsQueryHandler(repository.Object);

        // Act
        IReadOnlyList<ActiveMarketplaceListing> result = await handler.Handle(
            new GetActiveMarketplaceListingsQuery(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new ActiveMarketplaceListing(
                    listingId,
                    itemInstanceId,
                    "Obsidian Blade",
                    "WEAPON",
                    "Epic",
                    2,
                    42.5m,
                    sellerId
                )
            );
    }

    [Fact]
    public async Task Handle_NoActiveListings_ReturnsEmptyList()
    {
        // Arrange
        var repository = new Mock<IMarketplaceRepository>();
        repository
            .Setup(repository => repository.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new GetActiveMarketplaceListingsQueryHandler(repository.Object);

        // Act
        IReadOnlyList<ActiveMarketplaceListing> result = await handler.Handle(
            new GetActiveMarketplaceListingsQuery(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().BeEmpty();
    }
}
