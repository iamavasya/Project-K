using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreparatoryKurins : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// A підготовчий курінь has no number yet, so the number becomes optional and stays unique
        /// only among the kurins that have one. The waitlist learns whether an applicant leads such
        /// a kurin, and tells applications apart from the entries opened for members a провід added
        /// directly — which until now looked like applications that skipped review.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kurins_Number",
                table: "Kurins");

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "Kurins",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Kurins_Number",
                table: "Kurins",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.AddColumn<bool>(
                name: "IsPreparatoryKurin",
                table: "WaitlistEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "WaitlistEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Application");

            // The entries a провід's direct add opened: never a leader, no станиця, and approved in
            // the same instant they were requested. The form requires a станиця, so no application
            // matches all three.
            migrationBuilder.Sql("""
                UPDATE [WaitlistEntries]
                SET [Source] = 'DirectAdd'
                WHERE [IsKurinLeaderCandidate] = 0
                  AND [Stanytsia] IS NULL
                  AND [ApprovedAtUtc] = [RequestedAtUtc];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPreparatoryKurin",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Kurins_Number",
                table: "Kurins");

            // A kurin without a number cannot go back; it is refused rather than given a made-up one.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Kurins] WHERE [Number] IS NULL)
                    THROW 50000, 'Preparatory kurins exist; give each a number before rolling back.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "Kurins",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kurins_Number",
                table: "Kurins",
                column: "Number",
                unique: true);
        }
    }
}
