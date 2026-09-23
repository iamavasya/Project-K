using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMentorReassignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MentorAssignments_MentorUserKey_GroupKey",
                table: "MentorAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_MentorUserKey_GroupKey",
                table: "MentorAssignments",
                columns: new[] { "MentorUserKey", "GroupKey" },
                unique: true,
                filter: "[RevokedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MentorAssignments_MentorUserKey_GroupKey",
                table: "MentorAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_MentorUserKey_GroupKey",
                table: "MentorAssignments",
                columns: new[] { "MentorUserKey", "GroupKey" },
                unique: true);
        }
    }
}
