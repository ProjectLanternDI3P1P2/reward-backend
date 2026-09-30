using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddChestLootGenerationOutbox : Migration
{
    // Reuse the strict loot-table context columns without allocating during migration execution.
    private static readonly string[] LootTableContextColumns =
    [
        "source_type",
        "floor",
        "difficulty",
    ];

    // Reuse the chest business key columns that enforce one generation per run and chest.
    private static readonly string[] ChestGenerationBusinessKeyColumns =
    [
        "dungeon_run_id",
        "chest_id",
    ];

    // Reuse the pending-outbox lookup columns used by the dispatcher.
    private static readonly string[] PendingOutboxColumns =
    [
        "published_at_utc",
        "attempts",
        "occurred_at_utc",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "item_name_snapshot",
            table: "reward_item",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<string>(
            name: "item_rarity_snapshot",
            table: "reward_item",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<int>(
            name: "draw_count",
            table: "loot_table",
            type: "integer",
            nullable: false,
            defaultValue: 1
        );

        migrationBuilder.AddColumn<int>(
            name: "floor",
            table: "loot_table",
            type: "integer",
            nullable: true
        );

        migrationBuilder.CreateTable(
            name: "chest_loot_generation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                command_id = table.Column<Guid>(type: "uuid", nullable: false),
                dungeon_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                chest_id = table.Column<Guid>(type: "uuid", nullable: false),
                loot_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                floor = table.Column<int>(type: "integer", nullable: false),
                difficulty = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false
                ),
                reward_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_chest_loot_generation", x => x.id);
                table.CheckConstraint(
                    "ck_chest_loot_generation_difficulty_uppercase",
                    "difficulty = upper(difficulty)"
                );
                table.CheckConstraint("ck_chest_loot_generation_floor", "floor > 0");
                table.ForeignKey(
                    name: "FK_chest_loot_generation_loot_table_loot_table_id",
                    column: x => x.loot_table_id,
                    principalTable: "loot_table",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_chest_loot_generation_reward_reward_id",
                    column: x => x.reward_id,
                    principalTable: "reward",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "outbox_message",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                correlation_id = table.Column<string>(type: "text", nullable: false),
                causation_id = table.Column<string>(type: "text", nullable: true),
                type = table.Column<string>(type: "text", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
                producer = table.Column<string>(type: "text", nullable: false),
                payload = table.Column<byte[]>(type: "bytea", nullable: false),
                published_at_utc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true
                ),
                attempts = table.Column<int>(type: "integer", nullable: false),
                last_error = table.Column<string>(
                    type: "character varying(2000)",
                    maxLength: 2000,
                    nullable: true
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_message", x => x.id);
                table.CheckConstraint("ck_outbox_message_attempts", "attempts >= 0");
                table.CheckConstraint("ck_outbox_message_version", "version > 0");
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_loot_table_source_type_floor_difficulty",
            table: "loot_table",
            columns: LootTableContextColumns,
            unique: true,
            filter: "floor IS NOT NULL"
        );

        migrationBuilder.AddCheckConstraint(
            name: "ck_loot_table_draw_count",
            table: "loot_table",
            sql: "draw_count > 0"
        );

        migrationBuilder.AddCheckConstraint(
            name: "ck_loot_table_floor",
            table: "loot_table",
            sql: "floor IS NULL OR floor > 0"
        );

        migrationBuilder.CreateIndex(
            name: "IX_chest_loot_generation_dungeon_run_id_chest_id",
            table: "chest_loot_generation",
            columns: ChestGenerationBusinessKeyColumns,
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_chest_loot_generation_loot_table_id",
            table: "chest_loot_generation",
            column: "loot_table_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_chest_loot_generation_reward_id",
            table: "chest_loot_generation",
            column: "reward_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_outbox_message_published_at_utc_attempts_occurred_at_utc",
            table: "outbox_message",
            columns: PendingOutboxColumns,
            filter: "published_at_utc IS NULL"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "chest_loot_generation");

        migrationBuilder.DropTable(name: "outbox_message");

        migrationBuilder.DropIndex(
            name: "IX_loot_table_source_type_floor_difficulty",
            table: "loot_table"
        );

        migrationBuilder.DropCheckConstraint(name: "ck_loot_table_draw_count", table: "loot_table");

        migrationBuilder.DropCheckConstraint(name: "ck_loot_table_floor", table: "loot_table");

        migrationBuilder.DropColumn(name: "item_name_snapshot", table: "reward_item");

        migrationBuilder.DropColumn(name: "item_rarity_snapshot", table: "reward_item");

        migrationBuilder.DropColumn(name: "draw_count", table: "loot_table");

        migrationBuilder.DropColumn(name: "floor", table: "loot_table");
    }
}
