using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropMemberPlacementColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The columns are about to go, so anything they still say that Memberships does not is
            // about to be lost. Refuse rather than lose it: every placement was mirrored when the
            // membership table was introduced, and a mismatch here means something wrote a member
            // without announcing where they were put.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM Members m
    WHERE m.KurinKey IS NOT NULL
      AND m.KurinKey <> '00000000-0000-0000-0000-000000000000'
      AND NOT EXISTS (
            SELECT 1 FROM Memberships ms
            WHERE ms.MemberKey = m.MemberKey AND ms.KurinKey = m.KurinKey AND ms.LeftAtUtc IS NULL))
BEGIN
    THROW 51000, 'A member is still placed by their own record with no matching membership; the placement would be lost.', 1;
END;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Kurins_KurinKey",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_GroupKey",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_KurinKey",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "KurinKey",
                table: "Members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupKey",
                table: "Members",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KurinKey",
                table: "Members",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Members_GroupKey",
                table: "Members",
                column: "GroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_Members_KurinKey",
                table: "Members",
                column: "KurinKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Groups_GroupKey",
                table: "Members",
                column: "GroupKey",
                principalTable: "Groups",
                principalColumn: "GroupKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Kurins_KurinKey",
                table: "Members",
                column: "KurinKey",
                principalTable: "Kurins",
                principalColumn: "KurinKey");
        }
    }
}
