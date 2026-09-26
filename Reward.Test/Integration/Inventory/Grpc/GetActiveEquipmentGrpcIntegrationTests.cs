using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Reward.Contracts.V1;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Grpc;

public sealed class GetActiveEquipmentGrpcIntegrationTests(InventoryGrpcFixture fixture)
    : InventoryGrpcTestBase(fixture)
{
    [Fact]
    public async Task GetActiveEquipment_ReturnsOnlyEquippedItemsWithModifiersAndEmptySlots()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        await SeedActiveEquipmentAsync(heroId);
        RewardInventoryService.RewardInventoryServiceClient client = Fixture.CreateClient();

        // Act
        ActiveEquipmentReply response = await client
            .GetActiveEquipmentAsync(
                new GetActiveEquipmentRequest { HeroId = heroId.ToString() },
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;

        // Assert
        response.HeroId.Should().Be(heroId.ToString());
        response.Slots.Select(slot => slot.SlotName).Should().Equal("HEAD", "MAIN_HAND");
        response.Slots.Single(slot => slot.SlotName == "HEAD").EquippedItem.Should().BeNull();
        EquippedItem equippedItem = response
            .Slots.Single(slot => slot.SlotName == "MAIN_HAND")
            .EquippedItem;
        equippedItem.Should().NotBeNull();
        equippedItem.Type.Should().Be("WEAPON");
        equippedItem.Name.Should().Be("Obsidian Blade");
        equippedItem.Rarity.Should().Be("Epic");
        equippedItem
            .Modifiers.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new CombatModifier
                {
                    Name = "Brutality",
                    Stat = "ATTACK",
                    Value = 12.5,
                    Type = "FLAT",
                }
            );
    }

    [Fact]
    public async Task GetActiveEquipment_WhenHeroDoesNotExist_ReturnsNotFound()
    {
        RewardInventoryService.RewardInventoryServiceClient client = Fixture.CreateClient();

        Func<Task> act = async () =>
            await client
                .GetActiveEquipmentAsync(
                    new GetActiveEquipmentRequest { HeroId = Guid.NewGuid().ToString() },
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        RpcException exception = (await act.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
    }

    private async Task SeedActiveEquipmentAsync(Guid heroId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weaponCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
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
        var swordInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = sword,
            Inventory = inventory,
            Status = "AVAILABLE",
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var mainHand = new EquipmentSlot
        {
            Id = Guid.NewGuid(),
            Name = "MAIN_HAND",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var head = new EquipmentSlot
        {
            Id = Guid.NewGuid(),
            Name = "HEAD",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var modifier = new Modifier
        {
            Id = Guid.NewGuid(),
            Name = "Brutality",
            Stat = "ATTACK",
            Value = 12.5m,
            Type = "FLAT",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var itemModifier = new ItemModifier { Item = sword, Modifier = modifier };
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemInstance = swordInstance,
            Slot = mainHand,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(
            weaponCategory,
            epic,
            inventory,
            sword,
            swordInstance,
            mainHand,
            head,
            modifier,
            itemModifier,
            equipment
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
