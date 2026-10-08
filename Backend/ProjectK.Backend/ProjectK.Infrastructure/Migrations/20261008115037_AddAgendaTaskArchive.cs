using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaTaskArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskArchivePurgeAfterDays",
                table: "Kurins",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskAutoArchiveAfterDays",
                table: "Kurins",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "AgendaItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserKey",
                table: "AgendaItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "AgendaItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_KurinKey_ArchivedAtUtc",
                table: "AgendaItems",
                columns: new[] { "KurinKey", "ArchivedAtUtc" });

            // Existing kurins get the same defaults a new one does; empty would mean «switched off».
            migrationBuilder.Sql("UPDATE [Kurins] SET [TaskAutoArchiveAfterDays] = 30, [TaskArchivePurgeAfterDays] = 365;");

            // A task already done counts from its last change: when it was closed was never recorded.
            migrationBuilder.Sql("UPDATE [AgendaItems] SET [CompletedAtUtc] = [UpdatedDate] WHERE [Kind] = 1 AND [Status] = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgendaItems_KurinKey_ArchivedAtUtc",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "TaskArchivePurgeAfterDays",
                table: "Kurins");

            migrationBuilder.DropColumn(
                name: "TaskAutoArchiveAfterDays",
                table: "Kurins");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserKey",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "AgendaItems");
        }
    }
}
