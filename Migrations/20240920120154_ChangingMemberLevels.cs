using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_K.Migrations
{
    /// <inheritdoc />
    public partial class ChangingMemberLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MemberLevelId",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberLevelId",
                table: "Members");
        }
    }
}
