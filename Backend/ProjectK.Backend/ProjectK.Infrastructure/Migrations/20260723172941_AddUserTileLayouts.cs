using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTileLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTileLayouts",
                columns: table => new
                {
                    UserTileLayoutKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoardKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TileOrderJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTileLayouts", x => x.UserTileLayoutKey);
                    table.ForeignKey(
                        name: "FK_UserTileLayouts_AspNetUsers_UserKey",
                        column: x => x.UserKey,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTileLayouts_UserKey_BoardKey",
                table: "UserTileLayouts",
                columns: new[] { "UserKey", "BoardKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTileLayouts");
        }
    }
}
