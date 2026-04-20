using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CBTOnBoardingFlowMultiMentor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsZbtKurin",
                table: "Kurins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ZbtUserCap",
                table: "Kurins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBetaParticipant",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MentorAssignments",
                columns: table => new
                {
                    MentorAssignmentKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorAssignments", x => x.MentorAssignmentKey);
                    table.ForeignKey(
                        name: "FK_MentorAssignments_Groups_GroupKey",
                        column: x => x.GroupKey,
                        principalTable: "Groups",
                        principalColumn: "GroupKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    WaitlistEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsKurinLeaderCandidate = table.Column<bool>(type: "bit", nullable: false),
                    ClaimedKurinNameOrNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    VerificationChannel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerificationNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsBetaParticipant = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationAcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.WaitlistEntryKey);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    InvitationKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaitlistEntryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaitlistEntryKey1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.InvitationKey);
                    table.ForeignKey(
                        name: "FK_Invitations_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_WaitlistEntries_WaitlistEntryKey1",
                        column: x => x.WaitlistEntryKey1,
                        principalTable: "WaitlistEntries",
                        principalColumn: "WaitlistEntryKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TargetUserId",
                table: "Invitations",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_WaitlistEntryKey1",
                table: "Invitations",
                column: "WaitlistEntryKey1");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_GroupKey",
                table: "MentorAssignments",
                column: "GroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_MentorUserKey_GroupKey",
                table: "MentorAssignments",
                columns: new[] { "MentorUserKey", "GroupKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "MentorAssignments");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "IsZbtKurin",
                table: "Kurins");

            migrationBuilder.DropColumn(
                name: "ZbtUserCap",
                table: "Kurins");

            migrationBuilder.DropColumn(
                name: "IsBetaParticipant",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingStatus",
                table: "AspNetUsers");
        }
    }
}
