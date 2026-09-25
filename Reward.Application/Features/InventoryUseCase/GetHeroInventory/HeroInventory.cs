namespace Reward.Application.Features.InventoryUseCase.GetHeroInventory;

public sealed record HeroInventory(
    Guid HeroId,
    IReadOnlyList<InventoryItem> Items,
    IReadOnlyList<InventoryItem> Consumables);

public sealed record InventoryItem(
    Guid Id,
    string Type,
    string Name,
    string Rarity,
    string State,
    int Quantity,
    bool IsEquipped,
    bool IsReserved);
