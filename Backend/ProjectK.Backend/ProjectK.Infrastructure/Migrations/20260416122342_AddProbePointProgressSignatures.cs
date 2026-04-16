using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProbePointProgressSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProbePointProgresses",
                columns: table => new
                {
                    ProbePointProgressKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PointId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSigned = table.Column<bool>(type: "bit", nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbePointProgresses", x => x.ProbePointProgressKey);
                    table.ForeignKey(
                        name: "FK_ProbePointProgresses_Members_MemberKey",
                        column: x => x.MemberKey,
                        principalTable: "Members",
                        principalColumn: "MemberKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProbePointProgresses_MemberKey_ProbeId_PointId",
                table: "ProbePointProgresses",
                columns: new[] { "MemberKey", "ProbeId", "PointId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProbePointProgresses");
        }
    }
}
