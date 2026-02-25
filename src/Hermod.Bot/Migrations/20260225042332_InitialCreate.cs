using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Bot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordUserMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    HermodUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUserMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PostChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PlayersJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayPosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUserMappings_DiscordUserId",
                table: "DiscordUserMappings",
                column: "DiscordUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUserMappings_HermodUserId",
                table: "DiscordUserMappings",
                column: "HermodUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMappings_DiscordGuildId",
                table: "GuildMappings",
                column: "DiscordGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayPosts_GroupId_PlayId",
                table: "PlayPosts",
                columns: new[] { "GroupId", "PlayId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordUserMappings");

            migrationBuilder.DropTable(
                name: "GuildMappings");

            migrationBuilder.DropTable(
                name: "PlayPosts");
        }
    }
}
