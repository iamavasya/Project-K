using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropProgressMemberForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BadgeProgresses_Members_MemberKey",
                table: "BadgeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProbePointProgresses_Members_MemberKey",
                table: "ProbePointProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProbeProgresses_Members_MemberKey",
                table: "ProbeProgresses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_BadgeProgresses_Members_MemberKey",
                table: "BadgeProgresses",
                column: "MemberKey",
                principalTable: "Members",
                principalColumn: "MemberKey",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProbePointProgresses_Members_MemberKey",
                table: "ProbePointProgresses",
                column: "MemberKey",
                principalTable: "Members",
                principalColumn: "MemberKey",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProbeProgresses_Members_MemberKey",
                table: "ProbeProgresses",
                column: "MemberKey",
                principalTable: "Members",
                principalColumn: "MemberKey",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
