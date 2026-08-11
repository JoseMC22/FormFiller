using System.Globalization;
using ClosedXML.Excel;
using FormFiller.Core.Automation;

namespace FormFiller.Core.Reporting;

/// <summary>
/// Exports completed run results to a CSV file (RFC 4180) or an Excel workbook.
/// Both formats share the same column layout: Row, Status, Attempts, Message.
/// </summary>
public static class RunReportExporter
{
    private const string HeaderRow = "Row,Status,Attempts,Message";

    private const int ColumnCount = 4;

    /// <summary>
    /// Writes the results to <paramref name="writer"/> as RFC 4180 CSV with a header row and
    /// culture-invariant numbers.
    /// </summary>
    public static void ToCsv(IEnumerable<RunRowResult> results, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine(HeaderRow);

        foreach (var result in results)
        {
            writer.WriteLine(string.Join(
                ",",
                CsvField(result.RowNumber.ToString(CultureInfo.InvariantCulture)),
                CsvField(result.Success ? "OK" : "Failed"),
                CsvField(result.AttemptsUsed.ToString(CultureInfo.InvariantCulture)),
                CsvField(result.Message ?? string.Empty)));
        }

        writer.Flush();
    }

    /// <summary>
    /// Writes the results to a new Excel workbook with a styled header row.
    /// </summary>
    public static void ToExcel(IEnumerable<RunRowResult> results, string filePath)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Run Report");

        sheet.Cell(1, 1).Value = "Row";
        sheet.Cell(1, 2).Value = "Status";
        sheet.Cell(1, 3).Value = "Attempts";
        sheet.Cell(1, 4).Value = "Message";

        var headerRange = sheet.Range(1, 1, 1, ColumnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var rowNumber = 2;
        foreach (var result in results)
        {
            sheet.Cell(rowNumber, 1).Value = result.RowNumber;
            sheet.Cell(rowNumber, 2).Value = result.Success ? "OK" : "Failed";
            sheet.Cell(rowNumber, 3).Value = result.AttemptsUsed;
            sheet.Cell(rowNumber, 4).Value = result.Message ?? string.Empty;
            rowNumber++;
        }

        sheet.Columns(1, ColumnCount).AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private static string CsvField(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
