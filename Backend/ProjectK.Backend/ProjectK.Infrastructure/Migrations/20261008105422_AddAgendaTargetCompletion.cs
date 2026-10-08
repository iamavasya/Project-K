using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaTargetCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AgendaItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionMode",
                table: "AgendaAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AgendaAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAtUtc",
                table: "AgendaAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StatusChangedByUserKey",
                table: "AgendaAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgendaAssignmentProgress",
                columns: table => new
                {
                    AgendaAssignmentProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaAssignmentKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaAssignmentProgress", x => x.AgendaAssignmentProgressKey);
                    table.ForeignKey(
                        name: "FK_AgendaAssignmentProgress_AgendaAssignments_AgendaAssignmentKey",
                        column: x => x.AgendaAssignmentKey,
                        principalTable: "AgendaAssignments",
                        principalColumn: "AgendaAssignmentKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaAssignmentProgress_AgendaAssignmentKey_MemberKey",
                table: "AgendaAssignmentProgress",
                columns: new[] { "AgendaAssignmentKey", "MemberKey" },
                unique: true);

            // Every target of an existing task starts where the task stood: before this, the task had one
            // state for all of its targets. Who moved it and when were never recorded, so they stay empty.
            migrationBuilder.Sql(
                "UPDATE a SET a.[Status] = i.[Status] FROM [AgendaAssignments] a JOIN [AgendaItems] i ON i.[AgendaItemKey] = a.[AgendaItemKey];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaAssignmentProgress");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "CompletionMode",
                table: "AgendaAssignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AgendaAssignments");

            migrationBuilder.DropColumn(
                name: "StatusChangedAtUtc",
                table: "AgendaAssignments");

            migrationBuilder.DropColumn(
                name: "StatusChangedByUserKey",
                table: "AgendaAssignments");
        }
    }
}
