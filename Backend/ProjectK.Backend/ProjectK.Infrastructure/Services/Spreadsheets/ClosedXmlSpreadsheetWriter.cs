using ClosedXML.Excel;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.Infrastructure.Services.Spreadsheets;

/// <inheritdoc />
public sealed class ClosedXmlSpreadsheetWriter : ISpreadsheetWriter
{
    /// <summary>
    /// The one format a провід can open without being told how. CSV was the other candidate and lost:
    /// Excel reads it in the system codepage unless it finds a BOM, so Ukrainian names arrive as
    /// mojibake on most machines, and dates come out as text.
    /// </summary>
    public byte[] Write(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<SheetCell>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SafeSheetName(sheetName));

        for (var column = 0; column < headers.Count; column++)
        {
            var cell = sheet.Cell(1, column + 1);
            cell.Value = headers[column];
            cell.Style.Font.Bold = true;
        }

        for (var row = 0; row < rows.Count; row++)
        {
            var values = rows[row];
            for (var column = 0; column < values.Count; column++)
            {
                var cell = sheet.Cell(row + 2, column + 1);
                var value = values[column];

                if (value.Date is { } date)
                {
                    cell.Value = date.ToDateTime(TimeOnly.MinValue);
                    cell.Style.DateFormat.Format = "dd.MM.yyyy";
                }
                else if (!string.IsNullOrEmpty(value.Text))
                {
                    // As text, always: a phone number like 0500000103 would otherwise lose its
                    // leading zero the moment Excel decides it is a number.
                    cell.SetValue(value.Text);
                }
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Excel refuses some characters in a sheet name and caps it at 31.</summary>
    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(character => !"[]:*?/\\".Contains(character)).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
