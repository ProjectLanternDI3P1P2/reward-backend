using Bogus;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Seeding;

public static class RewardFakeDataGenerator
{
    private static readonly string[] CategoryLabels =
    [
        "WEAPON",
        "ARMOR",
        "SHIELD",
        "RING",
        "AMULET",
        "POTION",
        "VIAL",
    ];

    public static IReadOnlyList<Category> CreateCategories(DateTimeOffset now) =>
        CategoryLabels
            .Select(label => new Category
            {
                Id = Guid.NewGuid(),
                Label = label,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

    public static IReadOnlyList<Rarity> CreateRarities(DateTimeOffset now) =>
        [
            CreateRarity("Common", "#9CA3AF", 1, now),
            CreateRarity("Uncommon", "#22C55E", 2, now),
            CreateRarity("Rare", "#3B82F6", 3, now),
            CreateRarity("Epic", "#A855F7", 4, now),
            CreateRarity("Legendary", "#F59E0B", 5, now),
        ];

    public static IReadOnlyList<EquipmentSlot> CreateEquipmentSlots(DateTimeOffset now) =>
        new[] { "RIGHT_HAND", "LEFT_HAND", "BODY", "JEWELRY_1", "JEWELRY_2" }
            .Select(name => new EquipmentSlot
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

    public static IReadOnlyList<Item> GenerateItems(
        IReadOnlyList<Category> categories,
        IReadOnlyList<Rarity> rarities,
        DateTimeOffset now,
        int count = 40
    ) =>
        new Faker<Item>("en")
            .UseSeed(379)
            .RuleFor(item => item.Id, _ => Guid.NewGuid())
            .RuleFor(
                item => item.CategoryId,
                faker => categories[faker.Random.Int(0, categories.Count - 1)].Id
            )
            .RuleFor(
                item => item.RarityId,
                faker => rarities[faker.Random.Int(0, rarities.Count - 1)].Id
            )
            .RuleFor(
                item => item.Name,
                faker =>
                    $"{faker.Commerce.Color()} {faker.Commerce.ProductMaterial()} {faker.Commerce.ProductName()}"
            )
            .RuleFor(item => item.Description, faker => faker.Commerce.ProductDescription())
            .RuleFor(item => item.LevelRequired, faker => faker.Random.Int(1, 20))
            .RuleFor(
                item => item.Stackable,
                (_, item) =>
                    categories.Single(category => category.Id == item.CategoryId).Label
                        is "POTION"
                            or "VIAL"
            )
            .RuleFor(item => item.CreatedAt, _ => now)
            .RuleFor(item => item.UpdatedAt, _ => now)
            .Generate(count);

    public static IReadOnlyList<Inventory> CreateTestInventories(DateTimeOffset now) =>
        [
            CreateInventory("11111111-1111-1111-1111-111111111111", now),
            CreateInventory("22222222-2222-2222-2222-222222222222", now),
            CreateInventory("33333333-3333-3333-3333-333333333333", now),
        ];

    private static Rarity CreateRarity(string label, string color, int rank, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Label = label,
            Color = color,
            Rank = rank,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static Inventory CreateInventory(string heroId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.Parse(heroId),
            ItemCapacity = 40,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
