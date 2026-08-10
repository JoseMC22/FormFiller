using System.Diagnostics;
using System.Runtime.InteropServices;
using FormFiller.Core.Automation;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

[Collection("MuestraApp fixture")]
public sealed class RecipeRunnerTests
{
    [Fact]
    public void WaitForWindowByTitle_TimesOutForBogusTitle_Throws()
    {
        var hwnd = CreateHiddenTestWindow();
        try
        {
            var exception = Record.Exception(() =>
                RecipeRunner.WaitForWindowByTitle(
                    hwnd, "bogus-title-that-never-matches", TimeSpan.FromMilliseconds(300)));

            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Timed out waiting", invalidOperation.Message);
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [Fact]
    public void RunRecipe_SmokeTestWithMuestraApp()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Recipe Smoke");
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

            var recipe = new Recipe
            {
                Name = "Guardar y esperar",
                Steps =
                {
                    new RecipeStep { StepType = RecipeStepType.Wait, Value = "200", SortOrder = 0 },
                    new RecipeStep { StepType = RecipeStepType.ClickButton, Target = "btnGuardar", SortOrder = 1 }
                }
            };

            RecipeRunner.RunRecipe(hwnd, template, recipe, new Dictionary<string, string>());

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

    private static IntPtr CreateHiddenTestWindow()
    {
        var hwnd = CreateWindowExW(
            0, "STATIC", "FormFillerTestWindow", 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create a test window.");
        }

        return hwnd;
    }

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);
}
