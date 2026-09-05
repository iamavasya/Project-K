using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Members",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Branch",
                table: "Kurins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.MembershipKey);
                    table.ForeignKey(
                        name: "FK_Memberships_Groups_GroupKey",
                        column: x => x.GroupKey,
                        principalTable: "Groups",
                        principalColumn: "GroupKey");
                    table.ForeignKey(
                        name: "FK_Memberships_Kurins_KurinKey",
                        column: x => x.KurinKey,
                        principalTable: "Kurins",
                        principalColumn: "KurinKey");
                });


            // Every member gets the code another kurin can look them up by. Derived from their own
            // key so this matches what MemberPublicId.For produces for anyone added later, and so
            // re-running the migration cannot hand the same person a different code.
            migrationBuilder.Sql(@"
                UPDATE Members
                SET PublicId = 'PL-'
                    + UPPER(SUBSTRING(REPLACE(CONVERT(char(36), MemberKey), '-', ''), 1, 5))
                    + '-'
                    + UPPER(SUBSTRING(REPLACE(CONVERT(char(36), MemberKey), '-', ''), 6, 5))
                WHERE PublicId IS NULL OR PublicId = '';");

            // One membership per member, carrying where they already are. Kind is Youth for everyone:
            // today's kurins are all УПЮ and their виховники are entered as members too, so anything
            // else would silently change who appears in which list. Retagging is the провід's call.
            //
            // Members whose kurin no longer exists are skipped rather than pointed at a missing row —
            // an account activated without a kurin leaves Member.KurinKey empty.
            migrationBuilder.Sql(@"
                INSERT INTO Memberships
                    (MembershipKey, MemberKey, UserKey, KurinKey, GroupKey, Kind, JoinedAtUtc, LeftAtUtc, CreatedDate, UpdatedDate)
                SELECT NEWID(), m.MemberKey, m.UserKey, m.KurinKey, m.GroupKey, 0,
                       m.CreatedDate, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM Members m
                WHERE EXISTS (SELECT 1 FROM Kurins k WHERE k.KurinKey = m.KurinKey)
                  AND NOT EXISTS (
                        SELECT 1 FROM Memberships ms
                        WHERE ms.MemberKey = m.MemberKey
                          AND ms.KurinKey = m.KurinKey
                          AND ms.LeftAtUtc IS NULL);");

            migrationBuilder.CreateIndex(
                name: "IX_Members_PublicId",
                table: "Members",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_GroupKey",
                table: "Memberships",
                column: "GroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_KurinKey_LeftAtUtc",
                table: "Memberships",
                columns: new[] { "KurinKey", "LeftAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MemberKey_KurinKey",
                table: "Memberships",
                columns: new[] { "MemberKey", "KurinKey" },
                unique: true,
                filter: "[LeftAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MemberKey_LeftAtUtc",
                table: "Memberships",
                columns: new[] { "MemberKey", "LeftAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserKey_LeftAtUtc",
                table: "Memberships",
                columns: new[] { "UserKey", "LeftAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Members_PublicId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Kurins");
        }
    }
}
