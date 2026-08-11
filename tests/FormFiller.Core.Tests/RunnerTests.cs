using System.Diagnostics;
using FormFiller.Core.Automation;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

[Collection("MuestraApp fixture")]
public sealed class RunnerTests
{
    [Fact]
    public void BuildRowValues_MapsFieldsFromMatchingColumns()
    {
        var mappings = new List<FieldMapping>
        {
            new() { FieldName = "Codigo", ExcelColumn = "Codigo", SortOrder = 0 },
            new() { FieldName = "Nombre", ExcelColumn = "Nombre", SortOrder = 1 }
        };
        var columns = new List<string> { "Codigo", "Nombre", "Total" };
        var row = new List<string> { "A1", "Juan", "10" };

        var values = Runner.BuildRowValues(mappings, columns, row);

        Assert.Equal(2, values.Count);
        Assert.Equal("A1", values["Codigo"]);
        Assert.Equal("Juan", values["Nombre"]);
    }

    [Fact]
    public void BuildRowValues_SkipsMappingWithMissingColumn()
    {
        var mappings = new List<FieldMapping>
        {
            new() { FieldName = "Codigo", ExcelColumn = "Codigo" },
            new() { FieldName = "Nombre", ExcelColumn = "Inexistente" }
        };
        var columns = new List<string> { "Codigo", "Nombre" };
        var row = new List<string> { "A1", "Juan" };

        var values = Runner.BuildRowValues(mappings, columns, row);

        Assert.Single(values);
        Assert.Equal("A1", values["Codigo"]);
        Assert.False(values.ContainsKey("Nombre"));
    }

    [Fact]
    public void BuildRowValues_MatchesColumnsCaseInsensitively()
    {
        var mappings = new List<FieldMapping>
        {
            new() { FieldName = "Codigo", ExcelColumn = "CODIGO" },
            new() { FieldName = "Nombre", ExcelColumn = "nombre" }
        };
        var columns = new List<string> { "Codigo", "Nombre" };
        var row = new List<string> { "A1", "Juan" };

        var values = Runner.BuildRowValues(mappings, columns, row);

        Assert.Equal(2, values.Count);
        Assert.Equal("A1", values["Codigo"]);
        Assert.Equal("Juan", values["Nombre"]);
    }

    [Fact]
    public void FillFields_ReadBackAndClick_SmokeTestWithMuestraApp()
    {
        var exePath = MuestraAppFixture.ResolveExe();
        Assert.True(File.Exists(exePath), $"Fixture executable not found at '{exePath}'.");

        using var process = Process.Start(new ProcessStartInfo(exePath))!;
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Smoke");
            var codigoField = template.Fields.FirstOrDefault(f => f.Name == "Codigo");
            var nombreField = template.Fields.FirstOrDefault(f => f.Name == "Nombre");
            Assert.NotNull(codigoField);
            Assert.NotNull(nombreField);

            FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
            {
                ["Codigo"] = "ABC-123",
                ["Nombre"] = "Juan Pérez"
            });

            Assert.Equal("ABC-123", FormAutomation.ReadFieldValue(hwnd, codigoField));
            Assert.Equal("Juan Pérez", FormAutomation.ReadFieldValue(hwnd, nombreField));

            FormAutomation.ClickButton(hwnd, "btnGuardar");

            Assert.Equal(string.Empty, FormAutomation.ReadFieldValue(hwnd, codigoField));
            Assert.Equal(string.Empty, FormAutomation.ReadFieldValue(hwnd, nombreField));
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort cleanup of the fixture process.
                }
            }
        }
    }

    [Fact]
    public void RunRow_SingleAttempt_WhenRetriesDisabled_OnFailure()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var result = RunRowWithMissingField(hwnd, new RunOptions(null));

            Assert.False(result.Success);
            Assert.Equal(1, result.AttemptsUsed);
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void RunRow_Retries_OnFailure_UpToMaxRetries()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var options = new RunOptions(
                null,
                MaxRetriesPerRow: 2,
                RetryDelay: TimeSpan.FromMilliseconds(10));

            var result = RunRowWithMissingField(hwnd, options);

            Assert.False(result.Success);
            Assert.Equal(3, result.AttemptsUsed);
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void RunRow_SucceedsOnFirstAttempt_WhenRetriesEnabled()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Retry Smoke");
            var options = new RunOptions(
                null,
                MaxRetriesPerRow: 2,
                RetryDelay: TimeSpan.FromMilliseconds(10));

            var result = Runner.RunRow(
                hwnd,
                template,
                new Dictionary<string, string> { ["Codigo"] = "ABC-123" },
                options);

            Assert.True(result.Success);
            Assert.Equal(1, result.AttemptsUsed);
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void RunAll_PropagatesAttemptsUsedAndRowNumber()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var mappings = new List<FieldMapping>
            {
                new() { FieldName = "Inexistente", ExcelColumn = "A" }
            };
            var columns = new List<string> { "A" };
            var rows = new List<IReadOnlyList<string>>
            {
                new List<string> { "value" }
            };
            var options = new RunOptions(
                null,
                MaxRetriesPerRow: 1,
                RetryDelay: TimeSpan.FromMilliseconds(10));

            RunRowResult? reported = null;
            Runner.RunAll(
                hwnd,
                new FormTemplate { Name = "RunAll retry", Fields = { new FormField { Name = "Inexistente" } } },
                mappings,
                columns,
                rows,
                options,
                onRowDone: result => reported = result);

            Assert.NotNull(reported);
            Assert.False(reported!.Success);
            Assert.Equal(2, reported.RowNumber);
            Assert.Equal(2, reported.AttemptsUsed);
        }
        finally
        {
            Kill(process);
        }
    }

    private static RunRowResult RunRowWithMissingField(IntPtr hwnd, RunOptions options)
    {
        var template = new FormTemplate
        {
            Name = "Missing field retry test",
            Fields = { new FormField { Name = "Inexistente", FieldType = FieldType.Text } }
        };

        return Runner.RunRow(
            hwnd,
            template,
            new Dictionary<string, string> { ["Inexistente"] = "x" },
            options);
    }

    private static void Kill(Process process)
    {
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort cleanup of the fixture process.
            }
        }
    }
}
