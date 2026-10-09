using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaResponseOccurrence : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// An RSVP becomes an answer to one occurrence: a one-off event keeps NULL, a series stores the
        /// occurrence start, and the unique index widens to (item, user, occurrence). The rows that
        /// recurring items already carry were answers the system read as «the whole series»; nobody
        /// knows which сходини they were meant for, so they are removed rather than kept as a
        /// series-wide default — the reader has no such fallback on purpose. One-off answers stay as
        /// they are. Idempotent: every step checks before it writes.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('AgendaResponses', 'OccurrenceStartUtc') IS NULL
                    ALTER TABLE [AgendaResponses] ADD [OccurrenceStartUtc] datetime2 NULL;
                """);

            // Dynamic: the column was added a statement ago, and a static DELETE naming it would fail
            // to compile with the batch. RecurrenceFrequency 0 = None.
            migrationBuilder.Sql("""
                EXEC sp_executesql N'
                    DELETE r
                    FROM [AgendaResponses] r
                    JOIN [AgendaItems] i ON i.AgendaItemKey = r.AgendaItemKey
                    WHERE i.RecurrenceFrequency <> 0
                      AND r.OccurrenceStartUtc IS NULL';
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AgendaResponses_AgendaItemKey_UserKey' AND object_id = OBJECT_ID('AgendaResponses'))
                    DROP INDEX [IX_AgendaResponses_AgendaItemKey_UserKey] ON [AgendaResponses];
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AgendaResponses_AgendaItemKey_UserKey_OccurrenceStartUtc' AND object_id = OBJECT_ID('AgendaResponses'))
                    CREATE UNIQUE INDEX [IX_AgendaResponses_AgendaItemKey_UserKey_OccurrenceStartUtc]
                        ON [AgendaResponses] ([AgendaItemKey], [UserKey], [OccurrenceStartUtc]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgendaResponses_AgendaItemKey_UserKey_OccurrenceStartUtc",
                table: "AgendaResponses");

            migrationBuilder.DropColumn(
                name: "OccurrenceStartUtc",
                table: "AgendaResponses");

            migrationBuilder.CreateIndex(
                name: "IX_AgendaResponses_AgendaItemKey_UserKey",
                table: "AgendaResponses",
                columns: new[] { "AgendaItemKey", "UserKey" },
                unique: true);
        }
    }
}
