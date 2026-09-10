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
