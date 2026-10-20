using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps loot table context and draw configuration.</summary>
public sealed class LootTableConfiguration : IEntityTypeConfiguration<LootTable>
{
    public void Configure(EntityTypeBuilder<LootTable> builder)
    {
        // Keep nullable floors compatible while validating all newly contextualized tables.
        builder.ToTable(
            "loot_table",
            table =>
            {
                table.HasCheckConstraint("ck_loot_table_floor", "floor IS NULL OR floor > 0");
                table.HasCheckConstraint("ck_loot_table_draw_count", "draw_count > 0");
            }
        );
        // Map context and draw fields to the established snake_case schema.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Floor).HasColumnName("floor");
        builder.Property(x => x.Difficulty).HasColumnName("difficulty");
        builder.Property(x => x.SourceType).HasColumnName("source_type");
        builder.Property(x => x.DrawCount).HasColumnName("draw_count").HasDefaultValue(1);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        // Enforce one strict table per non-null source/floor/difficulty context.
        builder
            .HasIndex(x => new
            {
                x.SourceType,
                x.Floor,
                x.Difficulty,
            })
            .IsUnique()
            .HasFilter("floor IS NOT NULL");
        // Cascade table-owned rarity rules while preserving their catalogue references.
        builder
            .HasMany(x => x.RarityRules)
            .WithOne(x => x.LootTable)
            .HasForeignKey(x => x.LootTableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
