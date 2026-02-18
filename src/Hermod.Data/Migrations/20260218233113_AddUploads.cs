using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UploadId",
                table: "Plays",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UploadedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    FileBytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uploads_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plays_UploadId",
                table: "Plays",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_UploadedById",
                table: "Uploads",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Plays_Uploads_UploadId",
                table: "Plays",
                column: "UploadId",
                principalTable: "Uploads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plays_Uploads_UploadId",
                table: "Plays");

            migrationBuilder.DropTable(
                name: "Uploads");

            migrationBuilder.DropIndex(
                name: "IX_Plays_UploadId",
                table: "Plays");

            migrationBuilder.DropColumn(
                name: "UploadId",
                table: "Plays");
        }
    }
}
