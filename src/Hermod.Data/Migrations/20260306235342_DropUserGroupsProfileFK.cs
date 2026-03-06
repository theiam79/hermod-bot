using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hermod.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUserGroupsProfileFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_UserProfiles_UserId",
                table: "UserGroups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_UserProfiles_UserId",
                table: "UserGroups",
                column: "UserId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
