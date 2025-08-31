using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberEntity : Migration
    {
        /// <inheritdoc />
        private const string _members = "Members";
        private const string _nvarcharmax = "nvarchar(max)";
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: _members,
                columns: table => new
                {
                    MemberKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KurinKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: _nvarcharmax, nullable: false),
                    MiddleName = table.Column<string>(type: _nvarcharmax, nullable: false),
                    LastName = table.Column<string>(type: _nvarcharmax, nullable: false),
                    Email = table.Column<string>(type: _nvarcharmax, nullable: false),
                    PhoneNumber = table.Column<string>(type: _nvarcharmax, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.MemberKey);
                    table.ForeignKey(
                        name: "FK_Members_Groups_GroupKey",
                        column: x => x.GroupKey,
                        principalTable: "Groups",
                        principalColumn: "GroupKey",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Members_Kurins_KurinKey",
                        column: x => x.KurinKey,
                        principalTable: "Kurins",
                        principalColumn: "KurinKey");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_GroupKey",
                table: _members,
                column: "GroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_Members_KurinKey",
                table: _members,
                column: "KurinKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: _members);
        }
    }
}
