using FormFiller.Core.Models;

namespace FormFiller.Core.Automation;

public sealed record RunOptions(
    string? SubmitButtonName,
    int StartRowIndex = 0,
    int? EndRowIndex = null,
    TimeSpan? FieldDelay = null,
    TimeSpan? PostSubmitWait = null,
    Recipe? Recipe = null);

public sealed record RunRowResult(int RowNumber, bool Success, string Message);

public static class Runner
{
    public static IReadOnlyDictionary<string, string> BuildRowValues(
        IReadOnlyList<FieldMapping> mappings,
        IReadOnlyList<string> columns,
        IReadOnlyList<string> rowValues)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rowValues);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.ExcelColumn))
            {
                continue;
            }

            var columnIndex = FindColumnIndex(columns, mapping.ExcelColumn);
            if (columnIndex < 0 || columnIndex >= rowValues.Count)
            {
                continue;
            }

            result[mapping.FieldName] = rowValues[columnIndex];
        }

        return result;
    }

    public static RunRowResult RunRow(
        IntPtr hwnd,
        FormTemplate template,
        IReadOnlyDictionary<string, string> values,
        RunOptions options,
        IReadOnlyList<string>? columns = null,
        IReadOnlyList<string>? row = null,
        CancellationToken ct = default)
    {
        options ??= new RunOptions(null);

        try
        {
            FormAutomation.FillFields(hwnd, template, values);

            if (options.Recipe is not null)
            {
                if (columns is not null && row is not null)
                {
                    RecipeRunner.RunRecipe(hwnd, template, options.Recipe, values, columns, row, ct);
                }
                else
                {
                    RecipeRunner.RunRecipe(hwnd, template, options.Recipe, values, ct);
                }

                return new RunRowResult(0, true, "Row processed successfully.");
            }

            if (options.FieldDelay is { } fieldDelay && fieldDelay > TimeSpan.Zero)
            {
                Thread.Sleep(fieldDelay);
            }

            if (!string.IsNullOrWhiteSpace(options.SubmitButtonName))
            {
                FormAutomation.ClickButton(hwnd, options.SubmitButtonName);
            }

            if (options.PostSubmitWait is { } postSubmitWait && postSubmitWait > TimeSpan.Zero)
            {
                Thread.Sleep(postSubmitWait);
            }

            return new RunRowResult(0, true, "Row processed successfully.");
        }
        catch (Exception ex)
        {
            return new RunRowResult(0, false, ex.Message);
        }
    }

    public static void RunAll(
        IntPtr hwnd,
        FormTemplate template,
        IReadOnlyList<FieldMapping> mappings,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>> rows,
        RunOptions options,
        Action<RunRowResult>? onRowDone = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        options ??= new RunOptions(null);

        var startRowIndex = Math.Max(0, options.StartRowIndex);
        var endRowIndex = options.EndRowIndex.HasValue
            ? Math.Min(rows.Count - 1, options.EndRowIndex.Value)
            : rows.Count - 1;

        for (var index = startRowIndex; index <= endRowIndex; index++)
        {
            ct.ThrowIfCancellationRequested();

            var values = BuildRowValues(mappings, columns, rows[index]);
            var result = RunRow(hwnd, template, values, options, columns, row: rows[index], ct);

            // Excel row numbers are reported as index + 2 because row 1 is the header.
            onRowDone?.Invoke(new RunRowResult(index + 2, result.Success, result.Message));
        }
    }

    private static int FindColumnIndex(IReadOnlyList<string> columns, string excelColumn)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            if (string.Equals(columns[index], excelColumn, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
