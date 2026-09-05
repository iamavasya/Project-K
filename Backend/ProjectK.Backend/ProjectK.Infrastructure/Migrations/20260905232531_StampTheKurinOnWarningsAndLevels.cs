using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StampTheKurinOnWarningsAndLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KurinKey",
                table: "PlastLevelHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KurinKey",
                table: "MemberWarnings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));


            // Both stamps come from where the person is today. That is exact for пересторога — until
            // now a member could only be in one kurin, so the kurin that issued it is the one they are
            // in. For a level it is the best available answer and no worse than the nothing it replaces.
            migrationBuilder.Sql(@"
                UPDATE w
                SET w.KurinKey = m.KurinKey
                FROM MemberWarnings w
                INNER JOIN Members m ON m.MemberKey = w.MemberKey
                WHERE w.KurinKey = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.Sql(@"
                UPDATE h
                SET h.KurinKey = m.KurinKey
                FROM PlastLevelHistories h
                INNER JOIN Members m ON m.MemberKey = h.MemberKey
                WHERE h.KurinKey IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_KurinKey_RevokedAtUtc",
                table: "MemberWarnings",
                columns: new[] { "KurinKey", "RevokedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberWarnings_KurinKey_RevokedAtUtc",
                table: "MemberWarnings");

            migrationBuilder.DropColumn(
                name: "KurinKey",
                table: "PlastLevelHistories");

            migrationBuilder.DropColumn(
                name: "KurinKey",
                table: "MemberWarnings");
        }
    }
}
