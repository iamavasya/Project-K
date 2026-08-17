using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaCategoriesAndResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgendaCategoryKey",
                table: "AgendaItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgendaCategories",
                columns: table => new
                {
                    AgendaCategoryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    WaitlistEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RsvpRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ReminderLeadMinutes = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaCategories", x => x.AgendaCategoryKey);
                    table.ForeignKey(
                        name: "FK_AgendaCategories_Kurins_KurinKey",
                        column: x => x.KurinKey,
                        principalTable: "Kurins",
                        principalColumn: "KurinKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgendaResponses",
                columns: table => new
                {
                    AgendaResponseKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaResponses", x => x.AgendaResponseKey);
                    table.ForeignKey(
                        name: "FK_AgendaResponses_AgendaItems_AgendaItemKey",
                        column: x => x.AgendaItemKey,
                        principalTable: "AgendaItems",
                        principalColumn: "AgendaItemKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_AgendaCategoryKey",
                table: "AgendaItems",
                column: "AgendaCategoryKey");

            migrationBuilder.CreateIndex(
                name: "IX_AgendaCategories_KurinKey_IsArchived",
                table: "AgendaCategories",
                columns: new[] { "KurinKey", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaResponses_AgendaItemKey_UserKey",
                table: "AgendaResponses",
                columns: new[] { "AgendaItemKey", "UserKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems",
                column: "AgendaCategoryKey",
                principalTable: "AgendaCategories",
                principalColumn: "AgendaCategoryKey",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems");

            migrationBuilder.DropTable(
                name: "AgendaCategories");

            migrationBuilder.DropTable(
                name: "AgendaResponses");

            migrationBuilder.DropIndex(
                name: "IX_AgendaItems_AgendaCategoryKey",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "AgendaCategoryKey",
                table: "AgendaItems");
        }
    }
}
