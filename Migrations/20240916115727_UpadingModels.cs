using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_K.Migrations
{
    /// <inheritdoc />
    public partial class UpadingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_KurinLevels_KurinLevelId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Schools_SchoolId",
                table: "Members");

            migrationBuilder.AlterColumn<int>(
                name: "SchoolId",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "KurinLevelId",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Levels",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_KurinLevels_KurinLevelId",
                table: "Members",
                column: "KurinLevelId",
                principalTable: "KurinLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Schools_SchoolId",
                table: "Members",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_KurinLevels_KurinLevelId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Schools_SchoolId",
                table: "Members");

            migrationBuilder.AlterColumn<int>(
                name: "SchoolId",
                table: "Members",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KurinLevelId",
                table: "Members",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Name",
                table: "Levels",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_KurinLevels_KurinLevelId",
                table: "Members",
                column: "KurinLevelId",
                principalTable: "KurinLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Schools_SchoolId",
                table: "Members",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id");
        }
    }
}
