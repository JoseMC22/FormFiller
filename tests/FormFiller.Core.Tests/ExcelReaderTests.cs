using System.Globalization;
using ClosedXML.Excel;
using FormFiller.Core.Excel;

namespace FormFiller.Core.Tests;

public sealed class ExcelReaderTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public ExcelReaderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "FormFillerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "datos.xlsx");

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Datos");
        sheet.Cell(1, 1).Value = "Código";
        sheet.Cell(1, 2).Value = "Nombre";
        sheet.Cell(1, 3).Value = "Total";
        sheet.Cell(2, 1).Value = "A-001";
        sheet.Cell(2, 2).Value = "Juan Pérez";
        sheet.Cell(2, 3).Value = 1250.5;
        sheet.Cell(3, 1).Value = "B-002";
        sheet.Cell(3, 2).Value = "María García";
        sheet.Cell(3, 3).Value = 300;
        sheet.Cell(4, 1).Value = "C-003";
        sheet.Cell(4, 2).Value = "Carlos López";
        sheet.Cell(4, 3).Value = 99.99;

        workbook.AddWorksheet("Hoja2");
        workbook.SaveAs(_filePath);
    }

    [Fact]
    public void GetSheetNames_ReturnsAllSheetsInOrder()
    {
        var sheets = ExcelReader.GetSheetNames(_filePath);

        Assert.Equal(2, sheets.Count);
        Assert.Equal("Datos", sheets[0]);
        Assert.Equal("Hoja2", sheets[1]);
    }

    [Fact]
    public void GetColumns_ReturnsNonEmptyHeadersInOrder()
    {
        var columns = ExcelReader.GetColumns(_filePath, "Datos");

        Assert.Equal(new[] { "Código", "Nombre", "Total" }, columns.ToArray());
    }

    [Fact]
    public void GetPreviewRows_ReturnsUpToMaxRowsFromSecondRow()
    {
        var rows = ExcelReader.GetPreviewRows(_filePath, "Datos");

        Assert.Equal(3, rows.Count);

        Assert.Equal("A-001", rows[0][0]);
        Assert.Equal("Juan Pérez", rows[0][1]);
        var firstTotal = double.Parse(rows[0][2], NumberStyles.Any, CultureInfo.CurrentCulture);
        Assert.Equal(1250.5, firstTotal, 2);

        Assert.Equal("B-002", rows[1][0]);
        Assert.Equal("C-003", rows[2][0]);
        var lastTotal = double.Parse(rows[2][2], NumberStyles.Any, CultureInfo.CurrentCulture);
        Assert.Equal(99.99, lastTotal, 2);
    }

    [Fact]
    public void GetPreviewRows_RespectsMaxRowsLimit()
    {
        var rows = ExcelReader.GetPreviewRows(_filePath, "Datos", maxRows: 2);

        Assert.Equal(2, rows.Count);
        Assert.Equal("A-001", rows[0][0]);
        Assert.Equal("B-002", rows[1][0]);
    }

    [Fact]
    public void MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(_directory, "missing.xlsx");

        Assert.Throws<FileNotFoundException>(() => ExcelReader.GetSheetNames(missingPath));
        Assert.Throws<FileNotFoundException>(() => ExcelReader.GetColumns(missingPath, "Datos"));
        Assert.Throws<FileNotFoundException>(() => ExcelReader.GetPreviewRows(missingPath, "Datos"));
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
