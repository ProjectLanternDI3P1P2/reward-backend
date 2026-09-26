using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddNullableItemInstanceIdempotencyKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "idempotency_key",
            table: "item_instance",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_item_instance_idempotency_key",
            table: "item_instance",
            column: "idempotency_key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_item_instance_idempotency_key",
            table: "item_instance");

        migrationBuilder.DropColumn(
            name: "idempotency_key",
            table: "item_instance");
    }
}
