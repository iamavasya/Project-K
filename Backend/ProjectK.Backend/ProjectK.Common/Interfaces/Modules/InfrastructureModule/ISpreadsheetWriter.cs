namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

/// <summary>One cell. A date is kept as a date so the spreadsheet sorts and formats it as one.</summary>
public readonly record struct SheetCell(string? Text, DateOnly? Date)
{
    public static SheetCell Of(string? text) => new(text, null);

    public static SheetCell Of(DateOnly? date) => new(null, date);
}

/// <summary>
/// Turns a table into a spreadsheet file. Kept behind an interface because the format is a delivery
/// detail: what a registry contains is decided in the business layer, and how it is written is not.
/// </summary>
public interface ISpreadsheetWriter
{
    /// <summary>The bytes of one sheet, named, with a header row above the rows.</summary>
    byte[] Write(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<SheetCell>> rows);
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
