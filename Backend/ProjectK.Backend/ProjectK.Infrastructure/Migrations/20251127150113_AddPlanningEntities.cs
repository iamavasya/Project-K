using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningSessions",
                columns: table => new
                {
                    PlanningSessionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SearchEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    OptimalStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OptimalEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConflictScore = table.Column<double>(type: "float", nullable: false),
                    IsCalculated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSessions", x => x.PlanningSessionKey);
                    table.ForeignKey(
                        name: "FK_PlanningSessions_Kurins_KurinKey",
                        column: x => x.KurinKey,
                        principalTable: "Kurins",
                        principalColumn: "KurinKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningParticipants",
                columns: table => new
                {
                    PlanningParticipantKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanningSessionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleWeight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningParticipants", x => x.PlanningParticipantKey);
                    table.ForeignKey(
                        name: "FK_PlanningParticipants_PlanningSessions_PlanningSessionKey",
                        column: x => x.PlanningSessionKey,
                        principalTable: "PlanningSessions",
                        principalColumn: "PlanningSessionKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantBusyRanges",
                columns: table => new
                {
                    ParticipantBusyRangeKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanningParticipantKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantBusyRanges", x => x.ParticipantBusyRangeKey);
                    table.ForeignKey(
                        name: "FK_ParticipantBusyRanges_PlanningParticipants_PlanningParticipantKey",
                        column: x => x.PlanningParticipantKey,
                        principalTable: "PlanningParticipants",
                        principalColumn: "PlanningParticipantKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantBusyRanges_PlanningParticipantKey",
                table: "ParticipantBusyRanges",
                column: "PlanningParticipantKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningParticipants_PlanningSessionKey",
                table: "PlanningParticipants",
                column: "PlanningSessionKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSessions_KurinKey",
                table: "PlanningSessions",
                column: "KurinKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipantBusyRanges");

            migrationBuilder.DropTable(
                name: "PlanningParticipants");

            migrationBuilder.DropTable(
                name: "PlanningSessions");
        }
    }
}
