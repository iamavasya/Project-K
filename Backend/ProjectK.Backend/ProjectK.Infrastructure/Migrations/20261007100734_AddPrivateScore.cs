using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivateScoreCriteria",
                columns: table => new
                {
                    PrivateScoreCriterionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateScoreCriteria", x => x.PrivateScoreCriterionKey);
                });

            migrationBuilder.CreateTable(
                name: "PrivateScoreEntries",
                columns: table => new
                {
                    PrivateScoreEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrivateScoreCriterionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateScoreEntries", x => x.PrivateScoreEntryKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateScoreCriteria_KurinKey",
                table: "PrivateScoreCriteria",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateScoreEntries_KurinKey",
                table: "PrivateScoreEntries",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateScoreEntries_MembershipKey",
                table: "PrivateScoreEntries",
                column: "MembershipKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateScoreCriteria");

            migrationBuilder.DropTable(
                name: "PrivateScoreEntries");
        }
    }
}
