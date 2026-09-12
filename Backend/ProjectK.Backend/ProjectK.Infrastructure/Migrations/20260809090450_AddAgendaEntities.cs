using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendaItems",
                columns: table => new
                {
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaItems", x => x.AgendaItemKey);
                    table.ForeignKey(
                        name: "FK_AgendaItems_Kurins_KurinKey",
                        column: x => x.KurinKey,
                        principalTable: "Kurins",
                        principalColumn: "KurinKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgendaAssignments",
                columns: table => new
                {
                    AgendaAssignmentKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaAssignments", x => x.AgendaAssignmentKey);
                    table.ForeignKey(
                        name: "FK_AgendaAssignments_AgendaItems_AgendaItemKey",
                        column: x => x.AgendaItemKey,
                        principalTable: "AgendaItems",
                        principalColumn: "AgendaItemKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaAssignments_AgendaItemKey_TargetType_TargetKey",
                table: "AgendaAssignments",
                columns: new[] { "AgendaItemKey", "TargetType", "TargetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgendaAssignments_TargetType_TargetKey",
                table: "AgendaAssignments",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_KurinKey_StartUtc",
                table: "AgendaItems",
                columns: new[] { "KurinKey", "StartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaAssignments");

            migrationBuilder.DropTable(
                name: "AgendaItems");
        }
    }
}
