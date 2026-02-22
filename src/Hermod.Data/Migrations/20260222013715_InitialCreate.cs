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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllowSharing = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    FileContent = table.Column<string>(type: "jsonb", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BggId = table.Column<int>(type: "integer", nullable: true),
                    BggUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscribeToPlays = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    BgStatsPlayUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GameName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BggGameId = table.Column<int>(type: "integer", nullable: true),
                    GameThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DatePlayed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    LocationName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Rounds = table.Column<int>(type: "integer", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_Plays_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BgStatsPlayerUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MappedUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMappings_UserProfiles_MappedUserId",
                        column: x => x.MappedUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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
                        name: "FK_UserGroups_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayId = table.Column<Guid>(type: "uuid", nullable: false),
                    BgStatsPlayerUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MappedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Score = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CalculatedScore = table.Column<double>(type: "double precision", nullable: true),
                    Winner = table.Column<bool>(type: "boolean", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    Role = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Team = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NewPlayer = table.Column<bool>(type: "boolean", nullable: false),
                    StartPlayer = table.Column<bool>(type: "boolean", nullable: false)
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
                        name: "FK_PlayPlayers_UserProfiles_MappedUserId",
                        column: x => x.MappedUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_BgStatsPlayerUuid_MappedUserId",
                table: "PlayerMappings",
                columns: new[] { "BgStatsPlayerUuid", "MappedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMappings_MappedUserId",
                table: "PlayerMappings",
                column: "MappedUserId");

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
                name: "IX_Plays_UploadId",
                table: "Plays",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");
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
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Uploads");
        }
    }
}
