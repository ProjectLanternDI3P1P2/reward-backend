using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps immutable chest generations and their idempotency boundary.</summary>
public sealed class ChestLootGenerationConfiguration : IEntityTypeConfiguration<ChestLootGeneration>
{
    public void Configure(EntityTypeBuilder<ChestLootGeneration> builder)
    {
        // Enforce the same normalized context invariants as the application boundary.
        builder.ToTable(
            "chest_loot_generation",
            table =>
            {
                table.HasCheckConstraint("ck_chest_loot_generation_floor", "floor > 0");
                table.HasCheckConstraint(
                    "ck_chest_loot_generation_difficulty_uppercase",
                    "difficulty = upper(difficulty)"
                );
            }
        );

        // Keep persistence names aligned with the shared snake_case schema.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CommandId).HasColumnName("command_id");
        builder.Property(x => x.DungeonRunId).HasColumnName("dungeon_run_id");
        builder.Property(x => x.ChestId).HasColumnName("chest_id");
        builder.Property(x => x.LootTableId).HasColumnName("loot_table_id");
        builder.Property(x => x.Floor).HasColumnName("floor");
        builder.Property(x => x.Difficulty).HasColumnName("difficulty").HasMaxLength(50);
        builder.Property(x => x.RewardId).HasColumnName("reward_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Make the run/chest tuple the database-level idempotency guarantee.
        builder.HasIndex(x => new { x.DungeonRunId, x.ChestId }).IsUnique();
        builder.HasIndex(x => x.RewardId).IsUnique();

        // Retain the selected table and reward for audit-safe immutable replay.
        builder
            .HasOne(x => x.LootTable)
            .WithMany()
            .HasForeignKey(x => x.LootTableId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Reward)
            .WithOne()
            .HasForeignKey<ChestLootGeneration>(x => x.RewardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
