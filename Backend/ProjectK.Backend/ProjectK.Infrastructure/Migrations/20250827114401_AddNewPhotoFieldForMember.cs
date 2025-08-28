using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPhotoFieldForMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoBlobName",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoBlobName",
                table: "Members");
        }
    }
}
