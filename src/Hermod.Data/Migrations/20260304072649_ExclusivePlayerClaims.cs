using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExclusivePlayerClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid_MappedUserId",
                table: "PlayerMappings");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid",
                table: "PlayerMappings",
                column: "BgStatsPlayerUuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid",
                table: "PlayerMappings");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid_MappedUserId",
                table: "PlayerMappings",
                columns: new[] { "BgStatsPlayerUuid", "MappedUserId" },
                unique: true);
        }
    }
}
