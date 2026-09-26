using Bogus;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Seeding;

public static class DataSeeder
{
    /// <summary>
    /// Seeds reproducible development data for inventory browsing.
    /// It is safe to call repeatedly: an existing inventory prevents a second seed.
    /// </summary>
    public static async Task SeedAsync(
        RewardDbContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (await context.Inventories.AnyAsync(cancellationToken))
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<Category> categories = RewardFakeDataGenerator.CreateCategories(now);
        IReadOnlyList<Rarity> rarities = RewardFakeDataGenerator.CreateRarities(now);
        IReadOnlyList<EquipmentSlot> slots = RewardFakeDataGenerator.CreateEquipmentSlots(now);

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.Rarities.AddRangeAsync(rarities, cancellationToken);
        await context.EquipmentSlots.AddRangeAsync(slots, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Item> items = RewardFakeDataGenerator.GenerateItems(
            categories,
            rarities,
            now
        );
        await context.Items.AddRangeAsync(items, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Inventory> inventories = RewardFakeDataGenerator.CreateTestInventories(now);
        await context.Inventories.AddRangeAsync(inventories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        List<ItemInstance> instances = CreateItemInstances(inventories, items, categories, now);
        await context.ItemInstances.AddRangeAsync(instances, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        List<Equipment> equipment = CreateEquipment(inventories, instances, slots, now);
        await context.Equipment.AddRangeAsync(equipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<ItemInstance> CreateItemInstances(
        IReadOnlyList<Inventory> inventories,
        IReadOnlyList<Item> items,
        IReadOnlyList<Category> categories,
        DateTimeOffset now
    )
    {
        var faker = new Faker("en") { Random = new Randomizer(380) };
        var instances = new List<ItemInstance>();

        foreach (Inventory inventory in inventories)
        {
            foreach (Item item in faker.Random.Shuffle(items).Take(14))
            {
                string category = categories.Single(value => value.Id == item.CategoryId).Label;
                bool consumable = category is "POTION" or "VIAL";
                instances.Add(
                    new ItemInstance
                    {
                        Id = Guid.NewGuid(),
                        ItemId = item.Id,
                        InventoryId = inventory.Id,
                        Status = faker.Random.Bool(0.15f) ? "RESERVED" : "AVAILABLE",
                        Quantity = consumable ? faker.Random.Int(1, 10) : 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }
                );
            }
        }

        return instances;
    }

    private static List<Equipment> CreateEquipment(
        IReadOnlyList<Inventory> inventories,
        IReadOnlyList<ItemInstance> instances,
        IReadOnlyList<EquipmentSlot> slots,
        DateTimeOffset now
    )
    {
        var equipment = new List<Equipment>();

        foreach (Inventory inventory in inventories)
        {
            IEnumerable<ItemInstance> available = instances
                .Where(instance =>
                    instance.InventoryId == inventory.Id && instance.Status == "AVAILABLE"
                )
                .Take(2);

            foreach ((ItemInstance itemInstance, EquipmentSlot slot) in available.Zip(slots))
            {
                equipment.Add(
                    new Equipment
                    {
                        Id = Guid.NewGuid(),
                        HeroId = inventory.HeroId,
                        ItemInstanceId = itemInstance.Id,
                        SlotId = slot.Id,
                        Quantity = 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }
                );
            }
        }

        return equipment;
    }
}
