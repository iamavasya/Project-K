using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeniorKurinDues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KurinDuesRates_KurinKey_FromQuarter",
                table: "KurinDuesRates");

            migrationBuilder.AddColumn<Guid>(
                name: "TagKey",
                table: "KurinDuesRates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayeeKey",
                table: "DuesEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupKey",
                table: "DuesCharges",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "RateTagKey",
                table: "DuesCharges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KurinDuesPayees",
                columns: table => new
                {
                    KurinDuesPayeeKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KurinDuesPayees", x => x.KurinDuesPayeeKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KurinDuesRates_KurinKey_TagKey_FromQuarter",
                table: "KurinDuesRates",
                columns: new[] { "KurinKey", "TagKey", "FromQuarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KurinDuesPayees_KurinKey",
                table: "KurinDuesPayees",
                column: "KurinKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KurinDuesPayees");

            migrationBuilder.DropIndex(
                name: "IX_KurinDuesRates_KurinKey_TagKey_FromQuarter",
                table: "KurinDuesRates");

            migrationBuilder.DropColumn(
                name: "TagKey",
                table: "KurinDuesRates");

            migrationBuilder.DropColumn(
                name: "PayeeKey",
                table: "DuesEntries");

            migrationBuilder.DropColumn(
                name: "RateTagKey",
                table: "DuesCharges");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupKey",
                table: "DuesCharges",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KurinDuesRates_KurinKey_FromQuarter",
                table: "KurinDuesRates",
                columns: new[] { "KurinKey", "FromQuarter" },
                unique: true);
        }
    }
}
