using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdjustLeadershipEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leaderships_Type_EntityKey",
                table: "Leaderships");

            migrationBuilder.DropColumn(
                name: "EntityKey",
                table: "Leaderships");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupKey",
                table: "Leaderships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KurinKey",
                table: "Leaderships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leaderships_GroupKey",
                table: "Leaderships",
                column: "GroupKey",
                unique: true,
                filter: "[GroupKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderships_KurinKey",
                table: "Leaderships",
                column: "KurinKey");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderships_Type_KurinKey_GroupKey",
                table: "Leaderships",
                columns: new[] { "Type", "KurinKey", "GroupKey" });

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderships_Groups_GroupKey",
                table: "Leaderships",
                column: "GroupKey",
                principalTable: "Groups",
                principalColumn: "GroupKey",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderships_Kurins_KurinKey",
                table: "Leaderships",
                column: "KurinKey",
                principalTable: "Kurins",
                principalColumn: "KurinKey",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaderships_Groups_GroupKey",
                table: "Leaderships");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaderships_Kurins_KurinKey",
                table: "Leaderships");

            migrationBuilder.DropIndex(
                name: "IX_Leaderships_GroupKey",
                table: "Leaderships");

            migrationBuilder.DropIndex(
                name: "IX_Leaderships_KurinKey",
                table: "Leaderships");

            migrationBuilder.DropIndex(
                name: "IX_Leaderships_Type_KurinKey_GroupKey",
                table: "Leaderships");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "Leaderships");

            migrationBuilder.DropColumn(
                name: "KurinKey",
                table: "Leaderships");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityKey",
                table: "Leaderships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Leaderships_Type_EntityKey",
                table: "Leaderships",
                columns: new[] { "Type", "EntityKey" });
        }
    }
}
