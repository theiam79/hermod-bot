using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordMessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayPosts_Plays_PlayId",
                        column: x => x.PlayId,
                        principalTable: "Plays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayPosts_PlayId_DiscordGuildId",
                table: "PlayPosts",
                columns: new[] { "PlayId", "DiscordGuildId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayPosts");
        }
    }
}
