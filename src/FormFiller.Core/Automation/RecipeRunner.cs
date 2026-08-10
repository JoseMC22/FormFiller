using System.Diagnostics;
using FlaUI.Core.WindowsAPI;
using FormFiller.Core.Models;

namespace FormFiller.Core.Automation;

/// <summary>
/// Executes a <see cref="Recipe"/> step sequence against a captured form window.
/// </summary>
public static class RecipeRunner
{
    public static void RunRecipe(
        IntPtr hwnd,
        FormTemplate template,
        Recipe recipe,
        IReadOnlyDictionary<string, string> rowValues,
        IReadOnlyList<string> columns,
        IReadOnlyList<string> row,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(rowValues);

        foreach (var step in recipe.Steps.OrderBy(s => s.SortOrder))
        {
            ct.ThrowIfCancellationRequested();

            switch (step.StepType)
            {
                case RecipeStepType.SetField:
                    ExecuteSetField(hwnd, template, recipe, step, columns, row);
                    break;

                case RecipeStepType.ClickButton:
                    if (!string.IsNullOrWhiteSpace(step.Target))
                    {
                        FormAutomation.ClickButton(hwnd, step.Target);
                    }

                    break;

                case RecipeStepType.Wait:
                    Thread.Sleep(ParseInt(step.Value, 500));
                    break;

                case RecipeStepType.WaitForWindow:
                    WaitForWindowByTitle(hwnd, step.Target!, TimeSpan.FromMilliseconds(ParseInt(step.Value, 5000)));
                    break;

                case RecipeStepType.ClickIfWindowVisible:
                    try
                    {
                        WaitForWindowByTitle(hwnd, step.Target!, TimeSpan.FromMilliseconds(1500));
                        if (!string.IsNullOrWhiteSpace(step.Value))
                        {
                            FormAutomation.ClickButton(hwnd, step.Value!);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // The window did not appear within the probe window; continue silently.
                    }

                    break;
            }
        }
    }

    public static void RunRecipe(
        IntPtr hwnd,
        FormTemplate template,
        Recipe recipe,
        IReadOnlyDictionary<string, string> rowValues,
        CancellationToken ct = default)
    {
        RunRecipe(hwnd, template, recipe, rowValues, Array.Empty<string>(), Array.Empty<string>(), ct);
    }

    /// <summary>
    /// Polls the window title of the process hosting <paramref name="hwnd"/> every 100 ms until a title
    /// containing <paramref name="titleSubstring"/> is observed or <paramref name="timeout"/> elapses.
    /// </summary>
    /// <exception cref="InvalidOperationException">The window never appeared within the timeout.</exception>
    public static void WaitForWindowByTitle(IntPtr hwnd, string titleSubstring, TimeSpan timeout)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        if (string.IsNullOrWhiteSpace(titleSubstring))
        {
            throw new ArgumentException("A window title substring is required.", nameof(titleSubstring));
        }

        var processId = GetWindowProcessId(hwnd);
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                process.Refresh();
                var title = process.MainWindowTitle;
                if (!string.IsNullOrWhiteSpace(title) &&
                    title.Contains(titleSubstring, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch
            {
                // The process may have exited while polling; keep trying until the deadline.
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException(
            $"Timed out waiting for a window with title containing '{titleSubstring}'.");
    }

    private static void ExecuteSetField(
        IntPtr hwnd,
        FormTemplate template,
        Recipe recipe,
        RecipeStep step,
        IReadOnlyList<string> columns,
        IReadOnlyList<string> row)
    {
        if (string.IsNullOrWhiteSpace(step.Target))
        {
            throw new InvalidOperationException(
                $"SetField step in recipe '{recipe.Name}' is missing the target field name.");
        }

        var columnIndex = FindColumnIndex(columns, step.Value);
        if (columnIndex < 0 || columnIndex >= row.Count)
        {
            throw new InvalidOperationException(
                $"SetField step '{step.Target}' in recipe '{recipe.Name}' could not resolve column '{step.Value}'.");
        }

        FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
        {
            [step.Target] = row[columnIndex]
        });
    }

    private static int FindColumnIndex(IReadOnlyList<string> columns, string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return -1;
        }

        for (var index = 0; index < columns.Count; index++)
        {
            if (string.Equals(columns[index], column, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int GetWindowProcessId(IntPtr hwnd)
    {
        var success = User32.GetWindowThreadProcessId(hwnd, out var processId) != 0;
        if (!success)
        {
            throw new InvalidOperationException("Failed to resolve the window's process.");
        }

        return (int)processId;
    }
}
