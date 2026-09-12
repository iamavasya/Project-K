using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <summary>
    /// Public announcements are gone — the feature was removed rather than fixed, because nothing
    /// used it and the inbox covers what people actually read. This drops the drafts with it.
    /// <para>
    /// Whatever was in the table is lost, deliberately: it was release notes copied from GitHub, and
    /// GitHub still has them. Guarded so a database that never had the table, or one where an
    /// earlier run already dropped it, migrates the same either way.
    /// </para>
    /// </summary>
    public partial class DropPublicAnnouncementDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PublicAnnouncementDrafts]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[PublicAnnouncementDrafts];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicAnnouncementDrafts",
                columns: table => new
                {
                    PublicAnnouncementDraftKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    Codename = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageAltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageBlobKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImagePlacement = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastPublishError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParseMode = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RenderedText = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    SourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TelegramMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
    }
}
