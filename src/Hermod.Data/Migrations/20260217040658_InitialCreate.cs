using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DiscordGuildId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DiscordPostChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    AllowSharing = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DiscordId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    BggId = table.Column<int>(type: "INTEGER", nullable: true),
                    BggUsername = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubscribeToPlays = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BgStatsPlayerUuid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MappedUserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMappings_Users_MappedUserId",
                        column: x => x.MappedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerMappings_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UploadedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BgStatsPlayUuid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GameName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BggGameId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DatePlayed = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Rounds = table.Column<int>(type: "INTEGER", nullable: true),
                    Comments = table.Column<string>(type: "TEXT", nullable: true),
                    RawPlayFileJson = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plays_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Plays_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BgStatsPlayerUuid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlayerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MappedUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Score = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CalculatedScore = table.Column<double>(type: "REAL", nullable: true),
                    Winner = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Team = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NewPlayer = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartPlayer = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayPlayers_Plays_PlayId",
                        column: x => x.PlayId,
                        principalTable: "Plays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayPlayers_Users_MappedUserId",
                        column: x => x.MappedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_DiscordGuildId",
                table: "Groups",
                column: "DiscordGuildId",
                unique: true,
                filter: "DiscordGuildId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_MappedUserId",
                table: "PlayerMappings",
                column: "MappedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_OwnerUserId_BgStatsPlayerUuid",
                table: "PlayerMappings",
                columns: new[] { "OwnerUserId", "BgStatsPlayerUuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayPlayers_MappedUserId",
                table: "PlayPlayers",
                column: "MappedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayPlayers_PlayId",
                table: "PlayPlayers",
                column: "PlayId");

            migrationBuilder.CreateIndex(
                name: "IX_Plays_BgStatsPlayUuid",
                table: "Plays",
                column: "BgStatsPlayUuid");

            migrationBuilder.CreateIndex(
                name: "IX_Plays_DatePlayed",
                table: "Plays",
                column: "DatePlayed");

            migrationBuilder.CreateIndex(
                name: "IX_Plays_GroupId",
                table: "Plays",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Plays_UploadedById",
                table: "Plays",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordId",
                table: "Users",
                column: "DiscordId",
                unique: true,
                filter: "DiscordId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerMappings");

            migrationBuilder.DropTable(
                name: "PlayPlayers");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "Plays");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
