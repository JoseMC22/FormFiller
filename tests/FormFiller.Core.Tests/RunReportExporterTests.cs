using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FormFiller.Core.Automation;
using FormFiller.Core.Reporting;

namespace FormFiller.Core.Tests;

public sealed class RunReportExporterTests : IDisposable
{
    private static readonly RunRowResult[] SampleResults =
    {
        new(2, true, "Row processed successfully."),
        new(3, false, "Failed to fill the form: 'Nombre': element not found", 3)
    };

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "FormFillerTests", Guid.NewGuid().ToString("N"));

    public RunReportExporterTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void ToCsv_WritesHeaderAndDataRows()
    {
        var output = ToCsv(SampleResults);

        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Row,Status,Attempts,Message", lines[0]);
        Assert.Equal("2,OK,1,Row processed successfully.", lines[1]);
        Assert.Equal("3,Failed,3,Failed to fill the form: 'Nombre': element not found", lines[2]);
    }

    [Fact]
    public void ToCsv_QuotesAndEscapesRfc4180SpecialCharacters()
    {
        var results = new[] { new RunRowResult(4, false, "a,b,\"quoted\"\nsecond line", 2) };

        var output = ToCsv(results);

        Assert.Contains("\"a,b,\"\"quoted\"\"\nsecond line\"", output);
    }

    [Fact]
    public void ToCsv_UsesInvariantCultureForNumbers()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-AR");
            CultureInfo.CurrentUICulture = new CultureInfo("es-AR");

            var results = new[] { new RunRowResult(1234, true, "ok", 12) };

            var output = ToCsv(results);

            Assert.Contains("1234,OK,12,ok", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void ToCsv_EmptyResults_WritesOnlyHeader()
    {
        var output = ToCsv(Array.Empty<RunRowResult>());

        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Equal("Row,Status,Attempts,Message", lines[0]);
    }

    [Fact]
    public void ToExcel_WritesStyledHeaderAndDataRows()
    {
        var filePath = Path.Combine(_directory, "report.xlsx");

        RunReportExporter.ToExcel(SampleResults, filePath);

        using var workbook = new XLWorkbook(filePath);
        var sheet = Assert.Single(workbook.Worksheets);

        Assert.Equal("Row", sheet.Cell(1, 1).GetString());
        Assert.Equal("Status", sheet.Cell(1, 2).GetString());
        Assert.Equal("Attempts", sheet.Cell(1, 3).GetString());
        Assert.Equal("Message", sheet.Cell(1, 4).GetString());

        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);

        Assert.Equal("2", sheet.Cell(2, 1).GetString());
        Assert.Equal("OK", sheet.Cell(2, 2).GetString());
        Assert.Equal("1", sheet.Cell(2, 3).GetString());
        Assert.Equal("Row processed successfully.", sheet.Cell(2, 4).GetString());

        Assert.Equal("3", sheet.Cell(3, 1).GetString());
        Assert.Equal("Failed", sheet.Cell(3, 2).GetString());
        Assert.Equal("3", sheet.Cell(3, 3).GetString());
    }

    [Fact]
    public void ToExcel_UsesSameColumnLayoutAsCsv()
    {
        var csvHeaders = ToCsv(SampleResults)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[0]
            .Split(',');

        var filePath = Path.Combine(_directory, "report.xlsx");
        RunReportExporter.ToExcel(SampleResults, filePath);

        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.First();
        var excelHeaders = Enumerable.Range(1, 4).Select(c => sheet.Cell(1, c).GetString()).ToArray();

        Assert.Equal(csvHeaders, excelHeaders);
    }

    [Fact]
    public void ToExcel_ThrowsWhenFilePathIsBlank()
    {
        Assert.Throws<ArgumentException>(() => RunReportExporter.ToExcel(SampleResults, "  "));
    }

    private static string ToCsv(IEnumerable<RunRowResult> results)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            RunReportExporter.ToCsv(results, writer);
        }

        return builder.ToString();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup of temp files.
            }
        }
    }
}
