using Bogus;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Enums;

namespace Reward.Infrastructure.Persistence.Seeding;

public static class DataSeeder
{
    /// <summary>
    /// Seeds reproducible development data for inventory browsing and chest loot.
    /// Each data family is independently idempotent so existing development databases evolve.
    /// </summary>
    public static async Task SeedAsync(
        RewardDbContext context,
        CancellationToken cancellationToken = default
    )
    {
        // Seed reward sources independently so older development databases receive the catalogue.
        if (!await context.RewardSources.AnyAsync(cancellationToken))
        {
            await context.RewardSources.AddRangeAsync(
                RewardFakeDataGenerator.CreateRewardSources(),
                cancellationToken
            );
            await context.SaveChangesAsync(cancellationToken);
        }

        // Preserve existing inventory seed data while allowing later seed families to be added.
        if (!await context.Inventories.AnyAsync(cancellationToken))
        {
            await SeedInventoryDataAsync(context, cancellationToken);
        }

        // Seed chest contexts independently because older development databases already have inventories.
        await SeedChestLootDataAsync(context, cancellationToken);

        // Seed marketplace listings independently so older development databases receive browsable listings.
        await SeedMarketplaceDataAsync(context, cancellationToken);
    }

    private static async Task SeedInventoryDataAsync(
        RewardDbContext context,
        CancellationToken cancellationToken
    )
    {
        // Create the stable catalogue dimensions before items reference them.
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<Category> categories = RewardFakeDataGenerator.CreateCategories(now);
        IReadOnlyList<Rarity> rarities = RewardFakeDataGenerator.CreateRarities(now);
        IReadOnlyList<EquipmentSlot> slots = RewardFakeDataGenerator.CreateEquipmentSlots(now);

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.Rarities.AddRangeAsync(rarities, cancellationToken);
        await context.EquipmentSlots.AddRangeAsync(slots, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Generate the shared item catalogue after category and rarity rows exist.
        IReadOnlyList<Item> items = RewardFakeDataGenerator.GenerateItems(
            categories,
            rarities,
            now
        );
        await context.Items.AddRangeAsync(items, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Create browsable test inventories before their item instances.
        IReadOnlyList<Inventory> inventories = RewardFakeDataGenerator.CreateTestInventories(now);
        await context.Inventories.AddRangeAsync(inventories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Populate owned item stacks after both catalogue and inventory rows are durable.
        List<ItemInstance> instances = CreateItemInstances(inventories, items, categories, now);
        await context.ItemInstances.AddRangeAsync(instances, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Add a small equipped subset for the existing inventory read use cases.
        List<Equipment> equipment = CreateEquipment(inventories, instances, slots, now);
        await context.Equipment.AddRangeAsync(equipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedChestLootDataAsync(
        RewardDbContext context,
        CancellationToken cancellationToken
    )
    {
        // Ensure rewards can reference the stable CHEST source code.
        if (
            !await context.RewardSources.AnyAsync(
                source => source.Name == "CHEST",
                cancellationToken
            )
        )
        {
            context.RewardSources.Add(
                new RewardSource
                {
                    Id = Guid.NewGuid(),
                    Name = "CHEST",
                    Description = "Dungeon chest loot",
                }
            );
        }

        // Reuse the seeded catalogue so generated loot is immediately meaningful to developers.
        List<Item> items = await context
            .Items.Include(item => item.Rarity)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "Chest loot development data requires at least one catalogue item."
            );
        }

        // Read existing strict contexts so repeated starts only add missing combinations.
        var existingContexts = await context
            .LootTables.Where(table => table.SourceType == "CHEST" && table.Floor != null)
            .Select(table => new { table.Floor, table.Difficulty })
            .ToListAsync(cancellationToken);

        // Add exactly the missing floor/difficulty tables and their weighted children.
        IReadOnlyList<LootTable> candidateTables = RewardFakeDataGenerator.CreateChestLootTables(
            items,
            DateTimeOffset.UtcNow
        );
        foreach (
            LootTable table in candidateTables.Where(candidate =>
                !existingContexts.Any(existing =>
                    existing.Floor == candidate.Floor && existing.Difficulty == candidate.Difficulty
                )
            )
        )
        {
            context.LootTables.Add(table);
        }

        // Commit the source and all missing loot table graphs together.
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMarketplaceDataAsync(
        RewardDbContext context,
        CancellationToken cancellationToken
    )
    {
        // Preserve existing marketplace seed data while allowing reproducible development runs.
        if (await context.MarketplaceListings.AnyAsync(cancellationToken))
        {
            return;
        }

        List<Inventory> inventories = await context
            .Inventories.OrderBy(inventory => inventory.Id)
            .ToListAsync(cancellationToken);

        // Only unowned stacks can be listed, matching the active marketplace invariant.
        List<ItemInstance> availableInstances = await context
            .ItemInstances.Where(instance => instance.Status == ItemInstanceStatus.Available)
            .OrderBy(instance => instance.Id)
            .ToListAsync(cancellationToken);

        if (inventories.Count == 0 || availableInstances.Count == 0)
        {
            return;
        }

        IReadOnlyList<MarketplaceListing> listings =
            RewardFakeDataGenerator.CreateMarketplaceListings(
                inventories,
                availableInstances,
                DateTimeOffset.UtcNow
            );

        await context.MarketplaceListings.AddRangeAsync(listings, cancellationToken);
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
                string categoryLabel = categories
                    .Single(value => value.Id == item.CategoryId)
                    .Label;
                bool consumable =
                    Enum.TryParse(categoryLabel, ignoreCase: true, out ItemCategory category)
                    && category.IsConsumable();
                instances.Add(
                    new ItemInstance
                    {
                        Id = Guid.NewGuid(),
                        ItemId = item.Id,
                        InventoryId = inventory.Id,
                        Status = faker.Random.Bool(0.15f)
                            ? ItemInstanceStatus.Reserved
                            : ItemInstanceStatus.Available,
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
                    instance.InventoryId == inventory.Id
                    && instance.Status == ItemInstanceStatus.Available
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
