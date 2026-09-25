using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations;

/// <inheritdoc />
public partial class initial_migration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "category",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_category", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "class_tag",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_class_tag", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inventory",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                hero_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_capacity = table.Column<int>(type: "integer", nullable: false),
                potion_capacity = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory", x => x.id);
                table.CheckConstraint("ck_inventory_capacities", "item_capacity > 0 AND potion_capacity > 0");
            });

        migrationBuilder.CreateTable(
            name: "loot_table",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                difficulty = table.Column<string>(type: "text", nullable: false),
                source_type = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loot_table", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "modifier",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                stat = table.Column<string>(type: "text", nullable: false),
                value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_modifier", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "placeholder",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_placeholder", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "rarity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: false),
                rank = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rarity", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "reward_source",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reward_source", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "slot", columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_slot", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "trade",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                initiator_id = table.Column<Guid>(type: "uuid", nullable: false),
                counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                cash_adjustment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_trade", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "wallet",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wallet", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inventory_snapshot",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_snapshot", x => x.id);
                table.ForeignKey(
                    name: "FK_inventory_snapshot_inventory_inventory_id",
                    column: x => x.inventory_id,
                    principalTable: "inventory",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "item",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                rarity_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                level_required = table.Column<int>(type: "integer", nullable: false),
                stackable = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_item", x => x.id);
                table.ForeignKey(
                    name: "FK_item_category_category_id",
                    column: x => x.category_id,
                    principalTable: "category",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_item_rarity_rarity_id",
                    column: x => x.rarity_id,
                    principalTable: "rarity",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "loot_rarity_rule",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                loot_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                rarity_id = table.Column<Guid>(type: "uuid", nullable: false),
                weight = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loot_rarity_rule", x => x.id);
                table.CheckConstraint("ck_loot_rarity_rule_weight", "weight > 0");
                table.ForeignKey(
                    name: "FK_loot_rarity_rule_loot_table_loot_table_id",
                    column: x => x.loot_table_id,
                    principalTable: "loot_table",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_loot_rarity_rule_rarity_rarity_id",
                    column: x => x.rarity_id,
                    principalTable: "rarity",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "reward",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                reward_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                reward_key = table.Column<string>(type: "text", nullable: false),
                xp_amount = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reward", x => x.id);
                table.CheckConstraint("ck_reward_key_nonblank", "length(btrim(reward_key)) > 0");
                table.ForeignKey(
                    name: "FK_reward_reward_source_reward_source_id",
                    column: x => x.reward_source_id,
                    principalTable: "reward_source",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "run_inventory_session",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                phase = table.Column<string>(type: "text", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_run_inventory_session", x => x.id);
                table.ForeignKey(
                    name: "FK_run_inventory_session_inventory_snapshot_inventory_snapshot~",
                    column: x => x.inventory_snapshot_id,
                    principalTable: "inventory_snapshot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "item_class_tag",
            columns: table => new
            {
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                class_tag_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_item_class_tag", x => new { x.item_id, x.class_tag_id });
                table.ForeignKey(
                    name: "FK_item_class_tag_class_tag_class_tag_id",
                    column: x => x.class_tag_id,
                    principalTable: "class_tag",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_item_class_tag_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "item_instance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_item_instance", x => x.id);
                table.CheckConstraint("ck_item_instance_quantity", "quantity > 0");
                table.ForeignKey(
                    name: "FK_item_instance_inventory_inventory_id",
                    column: x => x.inventory_id,
                    principalTable: "inventory",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_item_instance_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "item_modifier",
            columns: table => new
            {
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                modifier_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_item_modifier", x => new { x.item_id, x.modifier_id });
                table.ForeignKey(
                    name: "FK_item_modifier_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_item_modifier_modifier_modifier_id",
                    column: x => x.modifier_id,
                    principalTable: "modifier",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "loot_table_entry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                loot_rarity_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                weight = table.Column<int>(type: "integer", nullable: false),
                min_quantity = table.Column<int>(type: "integer", nullable: false),
                max_quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loot_table_entry", x => x.id);
                table.CheckConstraint("ck_loot_table_entry_quantities", "weight > 0 AND min_quantity > 0 AND max_quantity >= min_quantity");
                table.ForeignKey(
                    name: "FK_loot_table_entry_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_loot_table_entry_loot_rarity_rule_loot_rarity_rule_id",
                    column: x => x.loot_rarity_rule_id,
                    principalTable: "loot_rarity_rule",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "equipment",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                hero_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_equipment", x => x.id);
                table.CheckConstraint("ck_equipment_quantity_one", "quantity = 1");
                table.ForeignKey(
                    name: "FK_equipment_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_equipment_slot_slot_id",
                    column: x => x.slot_id,
                    principalTable: "slot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "equipment_snapshot",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_equipment_snapshot", x => x.id);
                table.CheckConstraint("ck_equipment_snapshot_quantity_one", "quantity = 1");
                table.ForeignKey(
                    name: "FK_equipment_snapshot_inventory_snapshot_inventory_snapshot_id",
                    column: x => x.inventory_snapshot_id,
                    principalTable: "inventory_snapshot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_equipment_snapshot_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_equipment_snapshot_slot_slot_id",
                    column: x => x.slot_id,
                    principalTable: "slot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "item_snapshot",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_item_snapshot", x => x.id);
                table.CheckConstraint("ck_item_snapshot_quantity", "quantity >= 0");
                table.ForeignKey(
                    name: "FK_item_snapshot_inventory_snapshot_inventory_snapshot_id",
                    column: x => x.inventory_snapshot_id,
                    principalTable: "inventory_snapshot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_item_snapshot_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "marketplace_listing",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_marketplace_listing", x => x.id);
                table.ForeignKey(
                    name: "FK_marketplace_listing_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "reward_item",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                reward_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                quantity = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reward_item", x => x.id);
                table.CheckConstraint("ck_reward_item_quantity", "quantity > 0");
                table.ForeignKey(
                    name: "FK_reward_item_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_reward_item_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_reward_item_reward_reward_id",
                    column: x => x.reward_id,
                    principalTable: "reward",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "run_item_state",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_inventory_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                quantity = table.Column<int>(type: "integer", nullable: false),
                state = table.Column<string>(type: "text", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_run_item_state", x => x.id);
                table.CheckConstraint("ck_run_item_state_quantity", "quantity >= 0");
                table.ForeignKey(
                    name: "FK_run_item_state_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_run_item_state_item_item_id",
                    column: x => x.item_id,
                    principalTable: "item",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_run_item_state_run_inventory_session_run_inventory_session_~",
                    column: x => x.run_inventory_session_id,
                    principalTable: "run_inventory_session",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "trade_item",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                trade_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                side = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_trade_item", x => x.id);
                table.ForeignKey(
                    name: "FK_trade_item_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_trade_item_trade_trade_id",
                    column: x => x.trade_id,
                    principalTable: "trade",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "marketplace_reservation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_marketplace_reservation", x => x.id);
                table.ForeignKey(
                    name: "FK_marketplace_reservation_marketplace_listing_listing_id",
                    column: x => x.listing_id,
                    principalTable: "marketplace_listing",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "run_equipment_state",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_inventory_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                run_item_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_run_equipment_state", x => x.id);
                table.CheckConstraint("ck_run_equipment_quantity_one", "quantity = 1");
                table.ForeignKey(
                    name: "FK_run_equipment_state_run_inventory_session_run_inventory_ses~",
                    column: x => x.run_inventory_session_id,
                    principalTable: "run_inventory_session",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_run_equipment_state_run_item_state_run_item_state_id",
                    column: x => x.run_item_state_id,
                    principalTable: "run_item_state",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_run_equipment_state_slot_slot_id",
                    column: x => x.slot_id,
                    principalTable: "slot",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "marketplace_transaction",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                idempotency_key = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_marketplace_transaction", x => x.id);
                table.ForeignKey(
                    name: "FK_marketplace_transaction_marketplace_listing_listing_id",
                    column: x => x.listing_id,
                    principalTable: "marketplace_listing",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_marketplace_transaction_marketplace_reservation_reservation~",
                    column: x => x.reservation_id,
                    principalTable: "marketplace_reservation",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "ownership_transfer",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                from_owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                to_owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                from_inventory_id = table.Column<Guid>(type: "uuid", nullable: false),
                to_inventory_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ownership_transfer", x => x.id);
                table.ForeignKey(
                    name: "FK_ownership_transfer_inventory_from_inventory_id",
                    column: x => x.from_inventory_id,
                    principalTable: "inventory",
                    principalColumn: "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ownership_transfer_inventory_to_inventory_id",
                    column: x => x.to_inventory_id,
                    principalTable: "inventory",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ownership_transfer_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ownership_transfer_marketplace_transaction_transaction_id",
                    column: x => x.transaction_id,
                    principalTable: "marketplace_transaction",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "wallet_hold",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                trade_id = table.Column<Guid>(type: "uuid", nullable: true),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                hold_key = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wallet_hold", x => x.id);
                table.ForeignKey(
                    name: "FK_wallet_hold_marketplace_transaction_transaction_id",
                    column: x => x.transaction_id,
                    principalTable: "marketplace_transaction",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_wallet_hold_trade_trade_id",
                    column: x => x.trade_id,
                    principalTable: "trade",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_wallet_hold_wallet_wallet_id",
                    column: x => x.wallet_id,
                    principalTable: "wallet",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "wallet_ledger_entry",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                wallet_hold_id = table.Column<Guid>(type: "uuid", nullable: true),
                delta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                operation_key = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wallet_ledger_entry", x => x.id);
                table.ForeignKey(
                    name: "FK_wallet_ledger_entry_wallet_hold_wallet_hold_id",
                    column: x => x.wallet_hold_id,
                    principalTable: "wallet_hold",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_wallet_ledger_entry_wallet_wallet_id",
                    column: x => x.wallet_id,
                    principalTable: "wallet",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_equipment_hero_id_slot_id",
            table: "equipment",
            columns: new[] { "hero_id", "slot_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_equipment_item_instance_id",
            table: "equipment",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_equipment_slot_id",
            table: "equipment",
            column: "slot_id");

        migrationBuilder.CreateIndex(
            name: "IX_equipment_snapshot_inventory_snapshot_id",
            table: "equipment_snapshot",
            column: "inventory_snapshot_id");

        migrationBuilder.CreateIndex(
            name: "IX_equipment_snapshot_item_instance_id",
            table: "equipment_snapshot",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_equipment_snapshot_slot_id",
            table: "equipment_snapshot",
            column: "slot_id");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_snapshot_inventory_id_run_id",
            table: "inventory_snapshot",
            columns: new[] { "inventory_id", "run_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_item_category_id",
            table: "item",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_rarity_id",
            table: "item",
            column: "rarity_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_class_tag_class_tag_id",
            table: "item_class_tag",
            column: "class_tag_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_instance_inventory_id",
            table: "item_instance",
            column: "inventory_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_instance_item_id",
            table: "item_instance",
            column: "item_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_modifier_modifier_id",
            table: "item_modifier",
            column: "modifier_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_snapshot_inventory_snapshot_id",
            table: "item_snapshot",
            column: "inventory_snapshot_id");

        migrationBuilder.CreateIndex(
            name: "IX_item_snapshot_item_instance_id",
            table: "item_snapshot",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_loot_rarity_rule_loot_table_id",
            table: "loot_rarity_rule",
            column: "loot_table_id");

        migrationBuilder.CreateIndex(
            name: "IX_loot_rarity_rule_rarity_id",
            table: "loot_rarity_rule",
            column: "rarity_id");

        migrationBuilder.CreateIndex(
            name: "IX_loot_table_entry_item_id",
            table: "loot_table_entry",
            column: "item_id");

        migrationBuilder.CreateIndex(
            name: "IX_loot_table_entry_loot_rarity_rule_id",
            table: "loot_table_entry",
            column: "loot_rarity_rule_id");

        migrationBuilder.CreateIndex(
            name: "IX_marketplace_listing_item_instance_id",
            table: "marketplace_listing",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_marketplace_reservation_listing_id",
            table: "marketplace_reservation",
            column: "listing_id");

        migrationBuilder.CreateIndex(
            name: "IX_marketplace_transaction_listing_id",
            table: "marketplace_transaction",
            column: "listing_id");

        migrationBuilder.CreateIndex(
            name: "IX_marketplace_transaction_reservation_id",
            table: "marketplace_transaction",
            column: "reservation_id");

        migrationBuilder.CreateIndex(
            name: "IX_ownership_transfer_from_inventory_id",
            table: "ownership_transfer",
            column: "from_inventory_id");

        migrationBuilder.CreateIndex(
            name: "IX_ownership_transfer_item_instance_id",
            table: "ownership_transfer",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_ownership_transfer_to_inventory_id",
            table: "ownership_transfer",
            column: "to_inventory_id");

        migrationBuilder.CreateIndex(
            name: "IX_ownership_transfer_transaction_id",
            table: "ownership_transfer",
            column: "transaction_id");

        migrationBuilder.CreateIndex(
            name: "IX_reward_reward_key",
            table: "reward",
            column: "reward_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_reward_reward_source_id",
            table: "reward",
            column: "reward_source_id");

        migrationBuilder.CreateIndex(
            name: "IX_reward_item_item_id",
            table: "reward_item",
            column: "item_id");

        migrationBuilder.CreateIndex(
            name: "IX_reward_item_item_instance_id",
            table: "reward_item",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_reward_item_reward_id",
            table: "reward_item",
            column: "reward_id");

        migrationBuilder.CreateIndex(
            name: "IX_reward_source_name",
            table: "reward_source",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_run_equipment_state_run_inventory_session_id_slot_id",
            table: "run_equipment_state",
            columns: new[] { "run_inventory_session_id", "slot_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_run_equipment_state_run_item_state_id",
            table: "run_equipment_state",
            column: "run_item_state_id");

        migrationBuilder.CreateIndex(
            name: "IX_run_equipment_state_slot_id",
            table: "run_equipment_state",
            column: "slot_id");

        migrationBuilder.CreateIndex(
            name: "IX_run_inventory_session_inventory_snapshot_id",
            table: "run_inventory_session",
            column: "inventory_snapshot_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_run_item_state_item_id",
            table: "run_item_state",
            column: "item_id");

        migrationBuilder.CreateIndex(
            name: "IX_run_item_state_item_instance_id",
            table: "run_item_state",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_run_item_state_run_inventory_session_id",
            table: "run_item_state",
            column: "run_inventory_session_id");

        migrationBuilder.CreateIndex(
            name: "IX_slot_name",
            table: "slot",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_trade_item_item_instance_id",
            table: "trade_item",
            column: "item_instance_id");

        migrationBuilder.CreateIndex(
            name: "IX_trade_item_trade_id",
            table: "trade_item",
            column: "trade_id");

        migrationBuilder.CreateIndex(
            name: "IX_wallet_hold_trade_id",
            table: "wallet_hold",
            column: "trade_id");

        migrationBuilder.CreateIndex(
            name: "IX_wallet_hold_transaction_id",
            table: "wallet_hold",
            column: "transaction_id");

        migrationBuilder.CreateIndex(
            name: "IX_wallet_hold_wallet_id",
            table: "wallet_hold",
            column: "wallet_id");

        migrationBuilder.CreateIndex(
            name: "IX_wallet_ledger_entry_wallet_hold_id",
            table: "wallet_ledger_entry",
            column: "wallet_hold_id");

        migrationBuilder.CreateIndex(
            name: "IX_wallet_ledger_entry_wallet_id",
            table: "wallet_ledger_entry",
            column: "wallet_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "equipment");

        migrationBuilder.DropTable(
            name: "equipment_snapshot");

        migrationBuilder.DropTable(
            name: "item_class_tag");

        migrationBuilder.DropTable(
            name: "item_modifier");

        migrationBuilder.DropTable(
            name: "item_snapshot");

        migrationBuilder.DropTable(
            name: "loot_table_entry");

        migrationBuilder.DropTable(
            name: "ownership_transfer");

        migrationBuilder.DropTable(
            name: "placeholder");

        migrationBuilder.DropTable(
            name: "reward_item");

        migrationBuilder.DropTable(
            name: "run_equipment_state");

        migrationBuilder.DropTable(
            name: "trade_item");

        migrationBuilder.DropTable(
            name: "wallet_ledger_entry");

        migrationBuilder.DropTable(
            name: "class_tag");

        migrationBuilder.DropTable(
            name: "modifier");

        migrationBuilder.DropTable(
            name: "loot_rarity_rule");

        migrationBuilder.DropTable(
            name: "reward");

        migrationBuilder.DropTable(
            name: "run_item_state");

        migrationBuilder.DropTable(
            name: "slot");

        migrationBuilder.DropTable(
            name: "wallet_hold");

        migrationBuilder.DropTable(
            name: "loot_table");

        migrationBuilder.DropTable(
            name: "reward_source");

        migrationBuilder.DropTable(
            name: "run_inventory_session");

        migrationBuilder.DropTable(
            name: "marketplace_transaction");

        migrationBuilder.DropTable(
            name: "trade");

        migrationBuilder.DropTable(
            name: "wallet");

        migrationBuilder.DropTable(
            name: "inventory_snapshot");

        migrationBuilder.DropTable(
            name: "marketplace_reservation");

        migrationBuilder.DropTable(
            name: "marketplace_listing");

        migrationBuilder.DropTable(
            name: "item_instance");

        migrationBuilder.DropTable(
            name: "inventory");

        migrationBuilder.DropTable(
            name: "item");

        migrationBuilder.DropTable(
            name: "category");

        migrationBuilder.DropTable(
            name: "rarity");
    }
}
