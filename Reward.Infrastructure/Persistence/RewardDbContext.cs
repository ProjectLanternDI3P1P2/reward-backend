using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Infrastructure.Persistence;

public class RewardDbContext(DbContextOptions<RewardDbContext> options) : DbContext(options)
{
    public DbSet<Placeholder> Placeholders => Set<Placeholder>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemInstance> ItemInstances => Set<ItemInstance>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ClassTag> ClassTags => Set<ClassTag>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<Rarity> Rarities => Set<Rarity>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentSlot> EquipmentSlots => Set<EquipmentSlot>();
    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();
    public DbSet<ItemSnapshot> ItemSnapshots => Set<ItemSnapshot>();
    public DbSet<EquipmentSnapshot> EquipmentSnapshots => Set<EquipmentSnapshot>();
    public DbSet<RunInventorySession> RunInventorySessions => Set<RunInventorySession>();
    public DbSet<RunItemState> RunItemStates => Set<RunItemState>();
    public DbSet<RunEquipmentState> RunEquipmentStates => Set<RunEquipmentState>();
    public DbSet<RewardEntity> Rewards => Set<RewardEntity>();
    public DbSet<RewardItem> RewardItems => Set<RewardItem>();
    public DbSet<RewardSource> RewardSources => Set<RewardSource>();
    public DbSet<LootTable> LootTables => Set<LootTable>();
    public DbSet<LootRarityRule> LootRarityRules => Set<LootRarityRule>();
    public DbSet<LootTableEntry> LootTableEntries => Set<LootTableEntry>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<MarketplaceReservation> MarketplaceReservations => Set<MarketplaceReservation>();
    public DbSet<MarketplaceTransaction> MarketplaceTransactions => Set<MarketplaceTransaction>();
    public DbSet<OwnershipTransfer> OwnershipTransfers => Set<OwnershipTransfer>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletHold> WalletHolds => Set<WalletHold>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TradeItem> TradeItems => Set<TradeItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applique toutes les configurations d'entités automatiquement
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RewardDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
