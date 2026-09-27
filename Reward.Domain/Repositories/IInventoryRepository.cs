using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface IInventoryRepository
{
    Task<Inventory?> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken);

    Task<Inventory?> GetByHeroIdForUpdateAsync(Guid heroId, CancellationToken cancellationToken);

    Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipmentSlot>> GetEquipmentSlotsByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken
    );

    Task<bool> HasActiveMarketplaceListingAsync(
        Guid itemInstanceId,
        CancellationToken cancellationToken
    );

    Task<ItemInstance?> GetItemInstanceByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken
    );

    void AddItemInstance(ItemInstance itemInstance);

    void AddEquipment(Equipment equipment);

    void RemoveEquipment(Equipment equipment);

    void RemoveItemInstance(ItemInstance itemInstance);

    Task<ActiveEquipmentSnapshot?> GetActiveEquipmentByHeroIdAsync(
        Guid heroId,
        CancellationToken cancellationToken
    );
}
