using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KurinScoreSettings",
                columns: table => new
                {
                    KurinScoreSettingsKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Algorithm = table.Column<int>(type: "int", nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KurinScoreSettings", x => x.KurinScoreSettingsKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreAttendanceRates",
                columns: table => new
                {
                    ScoreAttendanceRateKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaCategoryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreAttendanceRates", x => x.ScoreAttendanceRateKey);
                    table.CheckConstraint("CK_ScoreAttendanceRates_OneTarget", "([AgendaCategoryKey] IS NULL AND [AgendaItemKey] IS NOT NULL) OR ([AgendaCategoryKey] IS NOT NULL AND [AgendaItemKey] IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "ScoreAttendances",
                columns: table => new
                {
                    ScoreAttendanceKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MarkedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreAttendances", x => x.ScoreAttendanceKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreEntries",
                columns: table => new
                {
                    ScoreEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScoreItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AgendaItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurrenceStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreEntries", x => x.ScoreEntryKey);
                    table.CheckConstraint("CK_ScoreEntries_OneTarget", "([MembershipKey] IS NULL AND [GroupKey] IS NOT NULL) OR ([MembershipKey] IS NOT NULL AND [GroupKey] IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "ScoreGroupMoves",
                columns: table => new
                {
                    ScoreGroupMoveKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromGroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToGroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreGroupMoves", x => x.ScoreGroupMoveKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreItems",
                columns: table => new
                {
                    ScoreItemKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreItems", x => x.ScoreItemKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreRules",
                columns: table => new
                {
                    ScoreRuleKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Variant = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreRules", x => x.ScoreRuleKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreStages",
                columns: table => new
                {
                    ScoreStageKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreStages", x => x.ScoreStageKey);
                });

            migrationBuilder.CreateTable(
                name: "ScoreTrailEvents",
                columns: table => new
                {
                    ScoreTrailEventKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubjectKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreTrailEvents", x => x.ScoreTrailEventKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KurinScoreSettings_KurinKey",
                table: "KurinScoreSettings",
                column: "KurinKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAttendanceRates_AgendaCategoryKey",
                table: "ScoreAttendanceRates",
                column: "AgendaCategoryKey",
                unique: true,
                filter: "[AgendaCategoryKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAttendanceRates_AgendaItemKey",
                table: "ScoreAttendanceRates",
                column: "AgendaItemKey",
                unique: true,
                filter: "[AgendaItemKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAttendanceRates_KurinKey",
                table: "ScoreAttendanceRates",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAttendances_KurinKey_AgendaItemKey",
                table: "ScoreAttendances",
                columns: new[] { "KurinKey", "AgendaItemKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAttendances_OnePerOccurrence",
                table: "ScoreAttendances",
                columns: new[] { "MembershipKey", "AgendaItemKey", "OccurrenceStartUtc" },
                unique: true,
                filter: "[RemovedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_ItemOncePerGroup",
                table: "ScoreEntries",
                columns: new[] { "GroupKey", "AgendaItemKey", "OccurrenceStartUtc", "ScoreItemKey" },
                unique: true,
                filter: "[GroupKey] IS NOT NULL AND [AgendaItemKey] IS NOT NULL AND [ScoreItemKey] IS NOT NULL AND [DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_ItemOncePerPerson",
                table: "ScoreEntries",
                columns: new[] { "MembershipKey", "AgendaItemKey", "OccurrenceStartUtc", "ScoreItemKey" },
                unique: true,
                filter: "[MembershipKey] IS NOT NULL AND [AgendaItemKey] IS NOT NULL AND [ScoreItemKey] IS NOT NULL AND [DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_KurinKey",
                table: "ScoreEntries",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreGroupMoves_KurinKey",
                table: "ScoreGroupMoves",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreGroupMoves_MembershipKey",
                table: "ScoreGroupMoves",
                column: "MembershipKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreItems_KurinKey",
                table: "ScoreItems",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreRules_KurinKey_Source_Variant_FromDate",
                table: "ScoreRules",
                columns: new[] { "KurinKey", "Source", "Variant", "FromDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreStages_KurinKey",
                table: "ScoreStages",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTrailEvents_KurinKey",
                table: "ScoreTrailEvents",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTrailEvents_SubjectKey",
                table: "ScoreTrailEvents",
                column: "SubjectKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KurinScoreSettings");

            migrationBuilder.DropTable(
                name: "ScoreAttendanceRates");

            migrationBuilder.DropTable(
                name: "ScoreAttendances");

            migrationBuilder.DropTable(
                name: "ScoreEntries");

            migrationBuilder.DropTable(
                name: "ScoreGroupMoves");

            migrationBuilder.DropTable(
                name: "ScoreItems");

            migrationBuilder.DropTable(
                name: "ScoreRules");

            migrationBuilder.DropTable(
                name: "ScoreStages");

            migrationBuilder.DropTable(
                name: "ScoreTrailEvents");
        }
    }
}
