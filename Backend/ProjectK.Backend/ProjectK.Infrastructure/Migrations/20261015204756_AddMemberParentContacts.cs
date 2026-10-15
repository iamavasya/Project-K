using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberParentContacts : Migration
    {
        private static readonly string[] Columns =
        [
            "MotherFirstName", "MotherLastName", "MotherPhone",
            "FatherFirstName", "FatherLastName", "FatherPhone"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.AddColumn<string>(
                    name: column,
                    table: "Members",
                    type: "nvarchar(max)",
                    nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.DropColumn(
                    name: column,
                    table: "Members");
            }
        }
    }
}
