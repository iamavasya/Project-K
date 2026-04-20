using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOnboardingAndMentorAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_AspNetUsers_TargetUserId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_WaitlistEntries_WaitlistEntryKey1",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TargetUserId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_WaitlistEntryKey1",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "WaitlistEntryKey1",
                table: "Invitations");

            migrationBuilder.AlterColumn<string>(
                name: "VerificationStatus",
                table: "WaitlistEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Email",
                table: "WaitlistEntries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TargetUserKey",
                table: "Invitations",
                column: "TargetUserKey");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_WaitlistEntryKey",
                table: "Invitations",
                column: "WaitlistEntryKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_AspNetUsers_TargetUserKey",
                table: "Invitations",
                column: "TargetUserKey",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_WaitlistEntries_WaitlistEntryKey",
                table: "Invitations",
                column: "WaitlistEntryKey",
                principalTable: "WaitlistEntries",
                principalColumn: "WaitlistEntryKey",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_AspNetUsers_TargetUserKey",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_WaitlistEntries_WaitlistEntryKey",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_Email",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TargetUserKey",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_Token",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_WaitlistEntryKey",
                table: "Invitations");

            migrationBuilder.AlterColumn<int>(
                name: "VerificationStatus",
                table: "WaitlistEntries",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "Invitations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WaitlistEntryKey1",
                table: "Invitations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TargetUserId",
                table: "Invitations",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_WaitlistEntryKey1",
                table: "Invitations",
                column: "WaitlistEntryKey1");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_AspNetUsers_TargetUserId",
                table: "Invitations",
                column: "TargetUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_WaitlistEntries_WaitlistEntryKey1",
                table: "Invitations",
                column: "WaitlistEntryKey1",
                principalTable: "WaitlistEntries",
                principalColumn: "WaitlistEntryKey",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
