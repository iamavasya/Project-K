namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

/// <summary>
/// One cell. A date is kept as a date and a count as a number, so the spreadsheet sorts, formats and
/// sums them as what they are instead of as text that merely looks like them.
/// </summary>
public readonly record struct SheetCell(string? Text, DateOnly? Date, int? Number)
{
    public static SheetCell Of(string? text) => new(text, null, null);

    public static SheetCell Of(DateOnly? date) => new(null, date, null);

    public static SheetCell Count(int number) => new(null, null, number);
}

/// <summary>One sheet: what it is called, its header row, and the rows under it.</summary>
public sealed record SheetTable(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<SheetCell>> Rows);

/// <summary>
/// Turns tables into a spreadsheet file. Kept behind an interface because the format is a delivery
/// detail: what a registry contains is decided in the business layer, and how it is written is not.
/// </summary>
public interface ISpreadsheetWriter
{
    /// <summary>
    /// The bytes of a workbook, one sheet per table, each with a bold header row. Several sheets
    /// rather than one because a реєстр is not one list: юнаки, впорядники and чисельність answer
    /// different questions and have different columns, and stacking them on one sheet would make
    /// every one of them unsortable.
    /// </summary>
    byte[] Write(IReadOnlyList<SheetTable> sheets);
}

/// <summary>One row as it was read, with the number the person sees in Excel.</summary>
public sealed record SheetRow(int Number, IReadOnlyList<string> Cells);

/// <summary>
/// Reads a spreadsheet back into rows of text. Everything arrives as a string on purpose: a roster
/// is written by people, and the same column can hold a date in one row and a note in the next —
/// deciding what a value means is the importer's job, not the reader's.
/// </summary>
public interface ISpreadsheetReader
{
    /// <summary>
    /// The first sheet, up to <paramref name="maxRows"/> rows. A cell Excel considers a date comes
    /// back as <c>yyyy-MM-dd</c> so it cannot be misread as day-first or month-first.
    /// </summary>
    IReadOnlyList<SheetRow> Read(Stream content, int maxRows);
}
