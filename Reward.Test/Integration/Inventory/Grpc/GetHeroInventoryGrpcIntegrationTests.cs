using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Reward.Contracts.V1;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Grpc;

public sealed class GetHeroInventoryGrpcIntegrationTests(InventoryGrpcFixture fixture)
    : InventoryGrpcTestBase(fixture)
{
    [Fact]
    public async Task GetHeroInventory_WhenInventoryExists_ReturnsTheVersionedContractResponse()
    {
        Guid heroId = Guid.NewGuid();
        Guid swordId = Guid.NewGuid();
        Guid potionId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, swordId, potionId);
        RewardInventoryService.RewardInventoryServiceClient client = Fixture.CreateClient();

        AsyncUnaryCall<HeroInventoryReply> call = client.GetHeroInventoryAsync(
            new GetHeroInventoryRequest { HeroId = heroId.ToString() },
            cancellationToken: TestContext.Current.CancellationToken
        );
        HeroInventoryReply response = await call.ResponseAsync;

        response
            .Should()
            .BeEquivalentTo(
                new HeroInventoryReply
                {
                    HeroId = heroId.ToString(),
                    Items =
                    {
                        new Reward.Contracts.V1.InventoryItem
                        {
                            Id = swordId.ToString(),
                            Type = "WEAPON",
                            Name = "Obsidian Blade",
                            Rarity = "Epic",
                            State = "AVAILABLE",
                            Quantity = 1,
                            IsEquipped = true,
                            IsReserved = false,
                        },
                    },
                    Consumables =
                    {
                        new Reward.Contracts.V1.InventoryItem
                        {
                            Id = potionId.ToString(),
                            Type = "POTION",
                            Name = "Health Potion",
                            Rarity = "Common",
                            State = "RESERVED",
                            Quantity = 3,
                            IsEquipped = false,
                            IsReserved = true,
                        },
                    },
                }
            );
        call.GetTrailers().GetValue("x-correlation-id").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetHeroInventory_WhenHeroDoesNotExist_ReturnsNotFound()
    {
        RewardInventoryService.RewardInventoryServiceClient client = Fixture.CreateClient();

        Func<Task> act = async () =>
            await client
                .GetHeroInventoryAsync(
                    new GetHeroInventoryRequest { HeroId = Guid.NewGuid().ToString() },
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        RpcException exception = (await act.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetHeroInventory_WhenHeroIdIsInvalid_ReturnsInvalidArgument()
    {
        RewardInventoryService.RewardInventoryServiceClient client = Fixture.CreateClient();

        Func<Task> act = async () =>
            await client
                .GetHeroInventoryAsync(
                    new GetHeroInventoryRequest { HeroId = "not-a-guid" },
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        RpcException exception = (await act.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    private async Task SeedInventoryAsync(Guid heroId, Guid swordId, Guid potionId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weaponCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potionCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var common = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Common",
            Color = "#FFFFFF",
            Rank = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var epic = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Epic",
            Color = "#A855F7",
            Rank = 4,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = 20,
            PotionCapacity = 10,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var sword = new Item
        {
            Id = Guid.NewGuid(),
            Category = weaponCategory,
            Rarity = epic,
            Name = "Obsidian Blade",
            Description = "A blade.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potion = new Item
        {
            Id = Guid.NewGuid(),
            Category = potionCategory,
            Rarity = common,
            Name = "Health Potion",
            Description = "A potion.",
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var swordInstance = new ItemInstance
        {
            Id = swordId,
            Item = sword,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potionInstance = new ItemInstance
        {
            Id = potionId,
            Item = potion,
            Inventory = inventory,
            Status = ItemInstanceStatus.Reserved,
            Quantity = 3,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var slot = new EquipmentSlot
        {
            Id = Guid.NewGuid(),
            Name = "MAIN_HAND",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemInstance = swordInstance,
            Slot = slot,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(
            weaponCategory,
            potionCategory,
            common,
            epic,
            inventory,
            sword,
            potion,
            swordInstance,
            potionInstance,
            slot,
            equipment
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
