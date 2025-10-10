using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContextForMemberRelationWithGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members",
                column: "GroupKey",
                principalTable: "Groups",
                principalColumn: "GroupKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members",
                column: "GroupKey",
                principalTable: "Groups",
                principalColumn: "GroupKey",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
