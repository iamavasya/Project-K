using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicAnnouncementDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicAnnouncementDrafts",
                columns: table => new
                {
                    PublicAnnouncementDraftKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codename = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    RenderedText = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    ParseMode = table.Column<int>(type: "int", nullable: false),
                    ImageBlobKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageAltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TelegramMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastPublishError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicAnnouncementDrafts", x => x.PublicAnnouncementDraftKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicAnnouncementDrafts_SourceType_SourceId",
                table: "PublicAnnouncementDrafts",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicAnnouncementDrafts_Status_CreatedAtUtc",
                table: "PublicAnnouncementDrafts",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicAnnouncementDrafts");
        }
    }
}
