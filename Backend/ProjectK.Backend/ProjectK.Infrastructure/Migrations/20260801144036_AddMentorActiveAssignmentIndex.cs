using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorActiveAssignmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_MentorUserKey",
                table: "MentorAssignments",
                column: "MentorUserKey",
                filter: "[RevokedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MentorAssignments_MentorUserKey",
                table: "MentorAssignments");
        }
    }
}
