using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDuesLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuesCharges",
                columns: table => new
                {
                    DuesChargeKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesCharges", x => x.DuesChargeKey);
                });

            migrationBuilder.CreateTable(
                name: "DuesConcessions",
                columns: table => new
                {
                    DuesConcessionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromQuarter = table.Column<int>(type: "int", nullable: false),
                    IsConcession = table.Column<bool>(type: "bit", nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesConcessions", x => x.DuesConcessionKey);
                });

            migrationBuilder.CreateTable(
                name: "DuesEntries",
                columns: table => new
                {
                    DuesEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    CounterMethod = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CollectedByMemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesEntries", x => x.DuesEntryKey);
                });

            migrationBuilder.CreateTable(
                name: "GroupDuesRates",
                columns: table => new
                {
                    GroupDuesRateKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromQuarter = table.Column<int>(type: "int", nullable: false),
                    GroupShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupDuesRates", x => x.GroupDuesRateKey);
                });

            migrationBuilder.CreateTable(
                name: "KurinDuesRates",
                columns: table => new
                {
                    KurinDuesRateKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromQuarter = table.Column<int>(type: "int", nullable: false),
                    StanytsiaFull = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StanytsiaReduced = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KurinShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SetByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KurinDuesRates", x => x.KurinDuesRateKey);
                });

            migrationBuilder.CreateTable(
                name: "DuesEntryEvents",
                columns: table => new
                {
                    DuesEntryEventKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DuesEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesEntryEvents", x => x.DuesEntryEventKey);
                    table.ForeignKey(
                        name: "FK_DuesEntryEvents_DuesEntries_DuesEntryKey",
                        column: x => x.DuesEntryKey,
                        principalTable: "DuesEntries",
                        principalColumn: "DuesEntryKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuesCharges_KurinKey",
                table: "DuesCharges",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_DuesCharges_MembershipKey_Quarter",
                table: "DuesCharges",
                columns: new[] { "MembershipKey", "Quarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuesConcessions_KurinKey",
                table: "DuesConcessions",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_DuesConcessions_MembershipKey_FromQuarter",
                table: "DuesConcessions",
                columns: new[] { "MembershipKey", "FromQuarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuesEntries_KurinKey_GroupKey",
                table: "DuesEntries",
                columns: new[] { "KurinKey", "GroupKey" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesEntries_MembershipKey",
                table: "DuesEntries",
                column: "MembershipKey");

            migrationBuilder.CreateIndex(
                name: "IX_DuesEntryEvents_DuesEntryKey",
                table: "DuesEntryEvents",
                column: "DuesEntryKey");

            migrationBuilder.CreateIndex(
                name: "IX_GroupDuesRates_GroupKey_FromQuarter",
                table: "GroupDuesRates",
                columns: new[] { "GroupKey", "FromQuarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupDuesRates_KurinKey",
                table: "GroupDuesRates",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_KurinDuesRates_KurinKey_FromQuarter",
                table: "KurinDuesRates",
                columns: new[] { "KurinKey", "FromQuarter" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuesCharges");

            migrationBuilder.DropTable(
                name: "DuesConcessions");

            migrationBuilder.DropTable(
                name: "DuesEntryEvents");

            migrationBuilder.DropTable(
                name: "GroupDuesRates");

            migrationBuilder.DropTable(
                name: "KurinDuesRates");

            migrationBuilder.DropTable(
                name: "DuesEntries");
        }
    }
}
