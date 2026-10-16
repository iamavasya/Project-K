using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKrai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kurins_Number",
                table: "Kurins");

            migrationBuilder.AddColumn<string>(
                name: "KraiCode",
                table: "Kurins",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "ua");

            migrationBuilder.CreateTable(
                name: "Krais",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Krais", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "KraiProfileRevisions",
                columns: table => new
                {
                    KraiProfileRevisionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KraiCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KraiProfileRevisions", x => x.KraiProfileRevisionKey);
                    table.ForeignKey(
                        name: "FK_KraiProfileRevisions_Krais_KraiCode",
                        column: x => x.KraiCode,
                        principalTable: "Krais",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Krais",
                columns: new[] { "Code", "CreatedAtUtc", "Name" },
                values: new object[,]
                {
                    { "cz", new DateTime(2026, 10, 14, 0, 0, 0, 0, DateTimeKind.Utc), "Чехія" },
                    { "ua", new DateTime(2026, 10, 14, 0, 0, 0, 0, DateTimeKind.Utc), "Україна" }
                });

            migrationBuilder.InsertData(
                table: "KraiProfileRevisions",
                columns: new[] { "KraiProfileRevisionKey", "CreatedAtUtc", "CreatedByUserKey", "Json", "KraiCode", "Note", "Revision", "SchemaVersion" },
                values: new object[,]
                {
                    { new Guid("6f0c1a52-3d4e-4b8a-9a71-2f3c4d5e6a01"), new DateTime(2026, 10, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, "{}", "ua", "Початковий профіль", 1, 1 },
                    { new Guid("6f0c1a52-3d4e-4b8a-9a71-2f3c4d5e6a02"), new DateTime(2026, 10, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, "{}", "cz", "Початковий профіль", 1, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kurins_KraiCode_Number",
                table: "Kurins",
                columns: new[] { "KraiCode", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KraiProfileRevisions_KraiCode_Revision",
                table: "KraiProfileRevisions",
                columns: new[] { "KraiCode", "Revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kurins_Krais_KraiCode",
                table: "Kurins",
                column: "KraiCode",
                principalTable: "Krais",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kurins_Krais_KraiCode",
                table: "Kurins");

            migrationBuilder.DropTable(
                name: "KraiProfileRevisions");

            migrationBuilder.DropTable(
                name: "Krais");

            migrationBuilder.DropIndex(
                name: "IX_Kurins_KraiCode_Number",
                table: "Kurins");

            migrationBuilder.DropColumn(
                name: "KraiCode",
                table: "Kurins");

            migrationBuilder.CreateIndex(
                name: "IX_Kurins_Number",
                table: "Kurins",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");
        }
    }
}
