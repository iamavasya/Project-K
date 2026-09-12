using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberProfileVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileVerificationNote",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileVerificationStatus",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileVerifiedAtUtc",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileVerifiedByUserKey",
                table: "Members",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileVerificationNote",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ProfileVerificationStatus",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ProfileVerifiedAtUtc",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ProfileVerifiedByUserKey",
                table: "Members");
        }
    }
}
