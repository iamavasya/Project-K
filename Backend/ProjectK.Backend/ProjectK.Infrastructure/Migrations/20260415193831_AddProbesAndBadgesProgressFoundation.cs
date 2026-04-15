using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProbesAndBadgesProgressFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgeProgresses",
                columns: table => new
                {
                    BadgeProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeProgresses", x => x.BadgeProgressKey);
                    table.ForeignKey(
                        name: "FK_BadgeProgresses_Members_MemberKey",
                        column: x => x.MemberKey,
                        principalTable: "Members",
                        principalColumn: "MemberKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProbeProgresses",
                columns: table => new
                {
                    ProbeProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedByRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedByRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbeProgresses", x => x.ProbeProgressKey);
                    table.ForeignKey(
                        name: "FK_ProbeProgresses_Members_MemberKey",
                        column: x => x.MemberKey,
                        principalTable: "Members",
                        principalColumn: "MemberKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BadgeProgressAuditEvents",
                columns: table => new
                {
                    BadgeProgressAuditEventKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeProgressAuditEvents", x => x.BadgeProgressAuditEventKey);
                    table.ForeignKey(
                        name: "FK_BadgeProgressAuditEvents_BadgeProgresses_BadgeProgressKey",
                        column: x => x.BadgeProgressKey,
                        principalTable: "BadgeProgresses",
                        principalColumn: "BadgeProgressKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProbeProgressAuditEvents",
                columns: table => new
                {
                    ProbeProgressAuditEventKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbeProgressAuditEvents", x => x.ProbeProgressAuditEventKey);
                    table.ForeignKey(
                        name: "FK_ProbeProgressAuditEvents_ProbeProgresses_ProbeProgressKey",
                        column: x => x.ProbeProgressKey,
                        principalTable: "ProbeProgresses",
                        principalColumn: "ProbeProgressKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeProgressAuditEvents_BadgeProgressKey",
                table: "BadgeProgressAuditEvents",
                column: "BadgeProgressKey");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeProgresses_MemberKey_BadgeId",
                table: "BadgeProgresses",
                columns: new[] { "MemberKey", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProbeProgressAuditEvents_ProbeProgressKey",
                table: "ProbeProgressAuditEvents",
                column: "ProbeProgressKey");

            migrationBuilder.CreateIndex(
                name: "IX_ProbeProgresses_MemberKey_ProbeId",
                table: "ProbeProgresses",
                columns: new[] { "MemberKey", "ProbeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeProgressAuditEvents");

            migrationBuilder.DropTable(
                name: "ProbeProgressAuditEvents");

            migrationBuilder.DropTable(
                name: "BadgeProgresses");

            migrationBuilder.DropTable(
                name: "ProbeProgresses");
        }
    }
}
