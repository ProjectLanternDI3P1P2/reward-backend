using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddConsumableUse : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "consumable_use",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                hero_id = table.Column<Guid>(type: "uuid", nullable: false),
                item_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                idempotency_key = table.Column<string>(type: "text", nullable: false),
                remaining_quantity = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_consumable_use", x => x.id);
                table.CheckConstraint(
                    "ck_consumable_use_remaining_quantity",
                    "remaining_quantity >= 0"
                );
                table.ForeignKey(
                    name: "FK_consumable_use_item_instance_item_instance_id",
                    column: x => x.item_instance_id,
                    principalTable: "item_instance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_consumable_use_idempotency_key",
            table: "consumable_use",
            column: "idempotency_key",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_consumable_use_item_instance_id",
            table: "consumable_use",
            column: "item_instance_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "consumable_use");
    }
}
