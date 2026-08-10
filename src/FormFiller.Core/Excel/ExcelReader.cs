using ClosedXML.Excel;

namespace FormFiller.Core.Excel;

public static class ExcelReader
{
    public static IReadOnlyList<string> GetSheetNames(string filePath)
    {
        EnsureFileExists(filePath);

        try
        {
            using var workbook = new XLWorkbook(filePath);
            return workbook.Worksheets.Select(ws => ws.Name).ToList();
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to read Excel workbook '{filePath}'.", ex);
        }
    }

    public static IReadOnlyList<string> GetColumns(string filePath, string sheetName)
    {
        EnsureFileExists(filePath);

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = GetWorksheet(workbook, sheetName, filePath);

            var headerRow = worksheet.FirstRowUsed();
            if (headerRow is null)
            {
                return Array.Empty<string>();
            }

            var (firstColumn, lastColumn) = GetColumnRange(worksheet, headerRow);

            var columns = new List<string>();
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var cell = headerRow.Cell(column);
                var value = cell.IsEmpty() ? string.Empty : cell.GetFormattedString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    columns.Add(value);
                }
            }

            return columns;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to read Excel workbook '{filePath}'.", ex);
        }
    }

    public static IReadOnlyList<IReadOnlyList<string>> GetPreviewRows(string filePath, string sheetName, int maxRows = 5)
    {
        EnsureFileExists(filePath);

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = GetWorksheet(workbook, sheetName, filePath);

            var headerRow = worksheet.FirstRowUsed();
            if (headerRow is null)
            {
                return Array.Empty<IReadOnlyList<string>>();
            }

            var (firstColumn, lastColumn) = GetColumnRange(worksheet, headerRow);

            var rows = new List<IReadOnlyList<string>>();
            var firstDataRow = headerRow.RowNumber() + 1;
            var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

            for (var rowNumber = firstDataRow; rowNumber <= lastDataRow && rows.Count < maxRows; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var values = new List<string>();
                var hasAnyValue = false;
                for (var column = firstColumn; column <= lastColumn; column++)
                {
                    var cell = row.Cell(column);
                    var value = cell.IsEmpty() ? string.Empty : cell.GetFormattedString();
                    values.Add(value);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasAnyValue = true;
                    }
                }

                if (!hasAnyValue)
                {
                    break;
                }

                rows.Add(values);
            }

            return rows;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to read Excel workbook '{filePath}'.", ex);
        }
    }

    private static void EnsureFileExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Excel file not found: '{filePath}'.", filePath);
        }
    }

    private static IXLWorksheet GetWorksheet(XLWorkbook workbook, string sheetName, string filePath)
    {
        return workbook.Worksheets.FirstOrDefault(ws => string.Equals(ws.Name, sheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found in '{filePath}'.");
    }

    private static (int First, int Last) GetColumnRange(IXLWorksheet worksheet, IXLRow headerRow)
    {
        var firstColumn = worksheet.FirstColumnUsed()?.ColumnNumber()
            ?? headerRow.FirstCellUsed()?.Address.ColumnNumber
            ?? 1;

        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber()
            ?? headerRow.LastCellUsed()?.Address.ColumnNumber
            ?? firstColumn;

        return (firstColumn, lastColumn);
    }
}
