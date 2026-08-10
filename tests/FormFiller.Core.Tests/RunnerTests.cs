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
}
