using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardDistributionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cause_id",
                table: "reward",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "distribution",
                table: "reward",
                type: "text",
                nullable: false,
                defaultValue: "INDIVIDUAL"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "recipient_player_id",
                table: "reward",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_reward_run_id",
                table: "reward",
                column: "run_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_reward_run_id", table: "reward");

            migrationBuilder.DropColumn(name: "cause_id", table: "reward");

            migrationBuilder.DropColumn(name: "distribution", table: "reward");

            migrationBuilder.DropColumn(name: "recipient_player_id", table: "reward");
        }
    }
}
