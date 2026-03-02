using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class DataLayerFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayPlayers_UserProfiles_MappedUserId",
                table: "PlayPlayers");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Plays",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plays_UploadedById",
                table: "Plays",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayPlayers_BgStatsPlayerUuid",
                table: "PlayPlayers",
                column: "BgStatsPlayerUuid");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayPlayers_UserProfiles_MappedUserId",
                table: "PlayPlayers",
                column: "MappedUserId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayPlayers_UserProfiles_MappedUserId",
                table: "PlayPlayers");

            migrationBuilder.DropIndex(
                name: "IX_Plays_UploadedById",
                table: "Plays");

            migrationBuilder.DropIndex(
                name: "IX_PlayPlayers_BgStatsPlayerUuid",
                table: "PlayPlayers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Plays");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayPlayers_UserProfiles_MappedUserId",
                table: "PlayPlayers",
                column: "MappedUserId",
                principalTable: "UserProfiles",
                principalColumn: "Id");
        }
    }
}
