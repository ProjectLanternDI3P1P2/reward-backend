using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportIdempotentRewardGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reward_item_item_instance_item_instance_id",
                table: "reward_item"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "hero_id",
                table: "reward",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_hero_id",
                table: "reward",
                column: "hero_id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_reward_item_item_instance_item_instance_id",
                table: "reward_item",
                column: "item_instance_id",
                principalTable: "item_instance",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reward_item_item_instance_item_instance_id",
                table: "reward_item"
            );

            migrationBuilder.DropIndex(name: "IX_reward_hero_id", table: "reward");

            migrationBuilder.DropColumn(name: "hero_id", table: "reward");

            migrationBuilder.AddForeignKey(
                name: "FK_reward_item_item_instance_item_instance_id",
                table: "reward_item",
                column: "item_instance_id",
                principalTable: "item_instance",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
