using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberWarnings",
                columns: table => new
                {
                    MemberWarningKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberWarnings", x => x.MemberWarningKey);
                    table.ForeignKey(
                        name: "FK_MemberWarnings_Members_MemberKey",
                        column: x => x.MemberKey,
                        principalTable: "Members",
                        principalColumn: "MemberKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_ExpiresAtUtc",
                table: "MemberWarnings",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_MemberKey_Level",
                table: "MemberWarnings",
                columns: new[] { "MemberKey", "Level" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberWarnings");
        }
    }
}
