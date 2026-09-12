using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    NotificationKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActorUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.NotificationKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RecipientUserKey_CreatedAtUtc",
                table: "AppNotifications",
                columns: new[] { "RecipientUserKey", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RecipientUserKey_DeduplicationKey",
                table: "AppNotifications",
                columns: new[] { "RecipientUserKey", "DeduplicationKey" },
                filter: "[DeduplicationKey] IS NOT NULL AND [ReadAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RecipientUserKey_ReadAtUtc",
                table: "AppNotifications",
                columns: new[] { "RecipientUserKey", "ReadAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotifications");
        }
    }
}
