using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpAfterOneZero : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// The clean-up after 1.0: the closed-beta flags go, and so does the kurin snapshot on the
        /// account. The one thing that snapshot still did — tell activation which kurin a founder's
        /// approval opened — moves to the waitlist entry first, and the drop is refused while any
        /// founder still waiting to activate would lose it. Idempotent: every step checks before it
        /// writes, and running it twice changes nothing.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('WaitlistEntries', 'FoundedKurinKey') IS NULL
                    ALTER TABLE [WaitlistEntries] ADD [FoundedKurinKey] uniqueidentifier NULL;
                """);

            // The founder's kurin, from the account to the entry its invitation hangs off.
            migrationBuilder.Sql("""
                IF COL_LENGTH('AspNetUsers', 'KurinKey') IS NOT NULL
                BEGIN
                    EXEC sp_executesql N'
                        UPDATE w SET w.FoundedKurinKey = u.KurinKey
                        FROM [WaitlistEntries] w
                        JOIN [Invitations] i ON i.WaitlistEntryKey = w.WaitlistEntryKey
                        JOIN [AspNetUsers] u ON u.Id = i.TargetUserKey
                        WHERE w.IsKurinLeaderCandidate = 1
                          AND w.FoundedKurinKey IS NULL
                          AND u.OnboardingStatus = 1
                          AND u.KurinKey IS NOT NULL';

                    -- Dynamic too: the column was added a statement ago, and a static SELECT
                    -- naming it would fail to compile with the batch.
                    EXEC sp_executesql N'
                        IF EXISTS (
                            SELECT 1
                            FROM [WaitlistEntries] w
                            JOIN [Invitations] i ON i.WaitlistEntryKey = w.WaitlistEntryKey
                            JOIN [AspNetUsers] u ON u.Id = i.TargetUserKey
                            WHERE w.IsKurinLeaderCandidate = 1
                              AND w.FoundedKurinKey IS NULL
                              AND u.OnboardingStatus = 1
                              AND u.KurinKey IS NOT NULL)
                            THROW 51000, ''CleanUpAfterOneZero: a founder still waiting to activate carries a kurin on the account that no waitlist entry took over. Move it to WaitlistEntries.FoundedKurinKey before AspNetUsers.KurinKey is dropped.'', 1;';
                END
                """);

            // A chosen kurin that no longer exists (STAB-05) would refuse the foreign key below.
            migrationBuilder.Sql("""
                UPDATE u SET u.ActiveKurinKey = NULL
                FROM [AspNetUsers] u
                WHERE u.ActiveKurinKey IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [Kurins] k WHERE k.KurinKey = u.ActiveKurinKey);
                """);

            // The flag columns carry the default constraints their AddColumn left behind, and a
            // column cannot go while one does — the migration rolled back on exactly that in dev.
            migrationBuilder.Sql("""
                DECLARE @table sysname, @column sysname, @constraint sysname;
                DECLARE columns CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t, c FROM (VALUES
                        ('WaitlistEntries', 'IsBetaParticipant'),
                        ('Kurins', 'IsZbtKurin'),
                        ('Kurins', 'ZbtUserCap'),
                        ('AspNetUsers', 'IsBetaParticipant'),
                        ('AspNetUsers', 'KurinKey')) AS v(t, c);
                OPEN columns;
                FETCH NEXT FROM columns INTO @table, @column;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF COL_LENGTH(@table, @column) IS NOT NULL
                    BEGIN
                        SET @constraint = NULL;
                        SELECT @constraint = dc.name
                        FROM sys.default_constraints dc
                        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(@table) AND c.name = @column;
                        IF @constraint IS NOT NULL
                            EXEC('ALTER TABLE [' + @table + '] DROP CONSTRAINT [' + @constraint + ']');
                        EXEC('ALTER TABLE [' + @table + '] DROP COLUMN [' + @column + ']');
                    END
                    FETCH NEXT FROM columns INTO @table, @column;
                END
                CLOSE columns;
                DEALLOCATE columns;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WaitlistEntries_FoundedKurinKey' AND object_id = OBJECT_ID('WaitlistEntries'))
                    CREATE INDEX [IX_WaitlistEntries_FoundedKurinKey] ON [WaitlistEntries] ([FoundedKurinKey]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_ActiveKurinKey' AND object_id = OBJECT_ID('AspNetUsers'))
                    CREATE INDEX [IX_AspNetUsers_ActiveKurinKey] ON [AspNetUsers] ([ActiveKurinKey]);
                IF OBJECT_ID('FK_AspNetUsers_Kurins_ActiveKurinKey', 'F') IS NULL
                    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Kurins_ActiveKurinKey]
                        FOREIGN KEY ([ActiveKurinKey]) REFERENCES [Kurins] ([KurinKey]) ON DELETE SET NULL;
                IF OBJECT_ID('FK_WaitlistEntries_Kurins_FoundedKurinKey', 'F') IS NULL
                    ALTER TABLE [WaitlistEntries] ADD CONSTRAINT [FK_WaitlistEntries_Kurins_FoundedKurinKey]
                        FOREIGN KEY ([FoundedKurinKey]) REFERENCES [Kurins] ([KurinKey]) ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Kurins_ActiveKurinKey",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Kurins_FoundedKurinKey",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_FoundedKurinKey",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ActiveKurinKey",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FoundedKurinKey",
                table: "WaitlistEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsBetaParticipant",
                table: "WaitlistEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsZbtKurin",
                table: "Kurins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ZbtUserCap",
                table: "Kurins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBetaParticipant",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "KurinKey",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
