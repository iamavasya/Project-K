using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAgendaCategoryCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgendaCategories_Kurins_KurinKey",
                table: "AgendaCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems");

            migrationBuilder.AddForeignKey(
                name: "FK_AgendaCategories_Kurins_KurinKey",
                table: "AgendaCategories",
                column: "KurinKey",
                principalTable: "Kurins",
                principalColumn: "KurinKey",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems",
                column: "AgendaCategoryKey",
                principalTable: "AgendaCategories",
                principalColumn: "AgendaCategoryKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgendaCategories_Kurins_KurinKey",
                table: "AgendaCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems");

            migrationBuilder.AddForeignKey(
                name: "FK_AgendaCategories_Kurins_KurinKey",
                table: "AgendaCategories",
                column: "KurinKey",
                principalTable: "Kurins",
                principalColumn: "KurinKey",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgendaItems_AgendaCategories_AgendaCategoryKey",
                table: "AgendaItems",
                column: "AgendaCategoryKey",
                principalTable: "AgendaCategories",
                principalColumn: "AgendaCategoryKey",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
