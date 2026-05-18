using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicAnnouncementImagePlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('PublicAnnouncementDrafts', 'ImagePlacement') IS NULL
                BEGIN
                    ALTER TABLE [PublicAnnouncementDrafts]
                    ADD [ImagePlacement] int NOT NULL
                        CONSTRAINT [DF_PublicAnnouncementDrafts_ImagePlacement] DEFAULT 0;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('PublicAnnouncementDrafts', 'ImagePlacement') IS NOT NULL
                BEGIN
                    DECLARE @constraintName nvarchar(128);

                    SELECT @constraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    INNER JOIN sys.tables t
                        ON t.object_id = c.object_id
                    WHERE t.name = N'PublicAnnouncementDrafts'
                        AND c.name = N'ImagePlacement';

                    IF @constraintName IS NOT NULL
                    BEGIN
                        EXEC(N'ALTER TABLE [PublicAnnouncementDrafts] DROP CONSTRAINT [' + @constraintName + N']');
                    END

                    ALTER TABLE [PublicAnnouncementDrafts] DROP COLUMN [ImagePlacement];
                END
                """);
        }
    }
}
