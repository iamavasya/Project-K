using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNewEntitiesForMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrentPlastLevel",
                table: "Members",
                newName: "LatestPlastLevel");

            migrationBuilder.CreateTable(
                name: "Leaderships",
                columns: table => new
                {
                    LeadershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EntityKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderships", x => x.LeadershipKey);
                });

            migrationBuilder.CreateTable(
                name: "LeadershipHistories",
                columns: table => new
                {
                    LeadershipHistoryKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadershipKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadershipHistories", x => x.LeadershipHistoryKey);
                    table.ForeignKey(
                        name: "FK_LeadershipHistories_Leaderships_LeadershipKey",
                        column: x => x.LeadershipKey,
                        principalTable: "Leaderships",
                        principalColumn: "LeadershipKey",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeadershipHistories_Members_MemberKey",
                        column: x => x.MemberKey,
                        principalTable: "Members",
                        principalColumn: "MemberKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadershipHistories_LeadershipKey_Role_StartDate",
                table: "LeadershipHistories",
                columns: new[] { "LeadershipKey", "Role", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadershipHistories_MemberKey_StartDate",
                table: "LeadershipHistories",
                columns: new[] { "MemberKey", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderships_Type_EntityKey",
                table: "Leaderships",
                columns: new[] { "Type", "EntityKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadershipHistories");

            migrationBuilder.DropTable(
                name: "Leaderships");

            migrationBuilder.RenameColumn(
                name: "LatestPlastLevel",
                table: "Members",
                newName: "CurrentPlastLevel");
        }
    }
}
