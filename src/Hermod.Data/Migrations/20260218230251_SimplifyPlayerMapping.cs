using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPlayerMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerMappings_Users_OwnerUserId",
                table: "PlayerMappings");

            migrationBuilder.DropIndex(
                name: "IX_PlayerMappings_OwnerUserId_BgStatsPlayerUuid",
                table: "PlayerMappings");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "PlayerMappings");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid_MappedUserId",
                table: "PlayerMappings",
                columns: new[] { "BgStatsPlayerUuid", "MappedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid_MappedUserId",
                table: "PlayerMappings");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "PlayerMappings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_OwnerUserId_BgStatsPlayerUuid",
                table: "PlayerMappings",
                columns: new[] { "OwnerUserId", "BgStatsPlayerUuid" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerMappings_Users_OwnerUserId",
                table: "PlayerMappings",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
