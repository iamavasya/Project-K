using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIndexFromPlastLevelHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlastLevelHistories_MemberKey_PlastLevel",
                table: "PlastLevelHistories");

            migrationBuilder.CreateIndex(
                name: "IX_PlastLevelHistories_MemberKey",
                table: "PlastLevelHistories",
                column: "MemberKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlastLevelHistories_MemberKey",
                table: "PlastLevelHistories");

            migrationBuilder.CreateIndex(
                name: "IX_PlastLevelHistories_MemberKey_PlastLevel",
                table: "PlastLevelHistories",
                columns: new[] { "MemberKey", "PlastLevel" },
                unique: true);
        }
    }
}
