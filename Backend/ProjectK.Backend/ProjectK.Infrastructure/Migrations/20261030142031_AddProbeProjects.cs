using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProbeProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProbeProjects",
                columns: table => new
                {
                    ProbeProjectKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsIndividual = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    DirectionMentorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DirectionMentorContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbeProjects", x => x.ProbeProjectKey);
                });

            migrationBuilder.CreateTable(
                name: "ProbeProjectImages",
                columns: table => new
                {
                    ProbeProjectImageKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeProjectKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbeProjectImages", x => x.ProbeProjectImageKey);
                    table.ForeignKey(
                        name: "FK_ProbeProjectImages_ProbeProjects_ProbeProjectKey",
                        column: x => x.ProbeProjectKey,
                        principalTable: "ProbeProjects",
                        principalColumn: "ProbeProjectKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeProjectImages_ProbeProjectKey",
                table: "ProbeProjectImages",
                column: "ProbeProjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_ProbeProjects_MemberKey_ProbeId",
                table: "ProbeProjects",
                columns: new[] { "MemberKey", "ProbeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProbeProjectImages");

            migrationBuilder.DropTable(
                name: "ProbeProjects");
        }
    }
}
