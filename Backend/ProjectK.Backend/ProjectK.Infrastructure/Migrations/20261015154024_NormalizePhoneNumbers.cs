using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <summary>
    /// Brings every stored phone into one shape, +380501234567: the separators are dropped and the
    /// Ukrainian numbers saved without «+» (0501234567, 380501234567, 00380…) get it. The
    /// all-zero placeholder an old activation wrote becomes empty. Anything else is left as it
    /// was, to be fixed by hand; nothing is lost.
    /// </summary>
    public partial class NormalizePhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Normalize("Members", "PhoneNumber", empty: "N''"));
            migrationBuilder.Sql(Normalize("WaitlistEntries", "PhoneNumber", empty: "N''"));
            migrationBuilder.Sql(Normalize("AspNetUsers", "PhoneNumber", empty: "NULL"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old spellings are not kept; the normalized numbers are valid in the old code too.
        }

        private static string Normalize(string table, string column, string empty) => $"""
            UPDATE t SET [{column}] = CASE
                WHEN x.c NOT LIKE '%[^0-9+]%' AND x.c NOT LIKE '%[1-9]%' THEN {empty}
                WHEN x.c LIKE '+[1-9]%' AND SUBSTRING(x.c, 2, 100) NOT LIKE '%[^0-9]%' AND LEN(x.c) BETWEEN 8 AND 20 THEN x.c
                WHEN x.c NOT LIKE '%[^0-9]%' AND x.c LIKE '00[1-9]%' AND LEN(x.c) BETWEEN 9 AND 21 THEN '+' + SUBSTRING(x.c, 3, 100)
                WHEN x.c NOT LIKE '%[^0-9]%' AND x.c LIKE '380%' AND LEN(x.c) = 12 THEN '+' + x.c
                WHEN x.c NOT LIKE '%[^0-9]%' AND x.c LIKE '0%' AND LEN(x.c) = 10 THEN '+38' + x.c
                ELSE t.[{column}]
            END
            FROM [{table}] t
            CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                t.[{column}], ' ', ''), NCHAR(160), ''), '-', ''), '(', ''), ')', ''), '.', ''), CHAR(9), '') AS c) x
            WHERE t.[{column}] IS NOT NULL AND t.[{column}] <> N'';
            """;
    }
}
