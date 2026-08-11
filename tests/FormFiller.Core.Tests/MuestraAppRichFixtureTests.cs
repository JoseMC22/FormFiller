using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FormFiller.Core.Automation;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

[Collection("MuestraApp fixture")]
public sealed class MuestraAppRichFixtureTests
{
    [Fact]
    public void CaptureWindow_DetectsAllRichControls()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);
            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Rich");

            foreach (var name in new[] { "Codigo", "Nombre", "Direccion", "Telefono", "Ciudad" })
            {
                AssertField(template, name, "Edit");
            }

            foreach (var name in new[] { "Email", "DNI", "CUIT", "Password", "Observations" })
            {
                AssertField(template, name, "Edit");
            }

            var country = AssertField(template, "Country", "ComboBox");
            Assert.Equal(FieldType.ComboBox, country.FieldType);

            var active = AssertField(template, "Active", "CheckBox");
            Assert.Equal(FieldType.CheckBox, active.FieldType);

            AssertField(template, "Person", "RadioButton");
            AssertField(template, "Company", "RadioButton");

            Assert.NotNull(template.Fields.FirstOrDefault(f => f.AutomationId == "dtpFechaAlta"));
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.AutomationId == "cboPais"));
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.AutomationId == "txtObservaciones"));
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.AutomationId == "txtPassword"));

            Assert.Equal(
                FieldType.Button,
                AssertField(template, "Guardar", "Button").FieldType);
            Assert.Equal(
                FieldType.Button,
                AssertField(template, "View Details", "Button").FieldType);
            Assert.Equal(
                FieldType.Button,
                AssertField(template, "Close Details", "Button").FieldType);

            var inputCount = template.Fields.Count(f =>
                f.ControlType is "Edit" or "ComboBox" or "CheckBox" or "RadioButton");
            Assert.True(inputCount >= 10, $"Expected at least 10 input controls, found {inputCount}.");
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void FillFields_ReadBackAndClear_AllFillableControlTypes()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);
            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Rich Fill");

            FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
            {
                ["Codigo"] = "EMP-001",
                ["Nombre"] = "Jane Doe",
                ["Email"] = "jane@example.com",
                ["DNI"] = "30111222",
                ["CUIT"] = "20301112223",
                ["Password"] = "Secret123",
                ["Observations"] = "First line\nSecond line",
                ["Country"] = "Chile",
                ["Active"] = "1"
            });

            Assert.Equal("EMP-001", ReadField(template, hwnd, "Codigo"));
            Assert.Equal("Jane Doe", ReadField(template, hwnd, "Nombre"));
            Assert.Equal("jane@example.com", ReadField(template, hwnd, "Email"));
            Assert.Equal("30111222", ReadField(template, hwnd, "DNI"));
            Assert.Equal("20301112223", ReadField(template, hwnd, "CUIT"));
            Assert.Equal("First line\nSecond line", ReadField(template, hwnd, "Observations"));
            Assert.Equal("Chile", ReadComboSelection(hwnd, "cboPais"));
            Assert.Equal(ToggleState.On, ReadCheckbox(hwnd, "chkActivo"));

            FormAutomation.ClickButton(hwnd, "btnGuardar");

            Assert.Equal(string.Empty, ReadField(template, hwnd, "Codigo"));
            Assert.Equal(string.Empty, ReadField(template, hwnd, "Nombre"));
            Assert.Equal(string.Empty, ReadField(template, hwnd, "Email"));
            Assert.Equal(string.Empty, ReadField(template, hwnd, "DNI"));
            Assert.Equal(string.Empty, ReadField(template, hwnd, "CUIT"));
            Assert.Equal(string.Empty, ReadField(template, hwnd, "Observations"));
            Assert.Null(ReadComboSelection(hwnd, "cboPais"));
            Assert.Equal(ToggleState.Off, ReadCheckbox(hwnd, "chkActivo"));
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void WaitForWindowByTitle_SecondWindow_AppearsAndDisappears()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);
            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            FormAutomation.ClickButton(hwnd, "btnVerDetalle");

            RecipeRunner.WaitForWindowByTitle(hwnd, "Details", TimeSpan.FromSeconds(5));

            var childWindow = UiInspector.GetOpenWindows()
                .FirstOrDefault(w => w.WindowTitle.Contains("Details"));
            Assert.NotNull(childWindow);

            var childTemplate = UiInspector.CaptureWindow(childWindow!.MainWindowHandle, "MuestraApp Detail");
            Assert.NotNull(childTemplate.Fields.FirstOrDefault(f => f.Name == "Detail"));
            Assert.NotNull(childTemplate.Fields.FirstOrDefault(f => f.AutomationId == "btnCerrar"));

            FormAutomation.ClickButton(childWindow.MainWindowHandle, "btnCerrar");

            var gone = Assert.Throws<InvalidOperationException>(() =>
                RecipeRunner.WaitForWindowByTitle(hwnd, "Details", TimeSpan.FromMilliseconds(1500)));
            Assert.Contains("Timed out waiting", gone.Message);
        }
        finally
        {
            Kill(process);
        }
    }

    [Fact]
    public void ClickIfWindowVisibleRecipe_ClosesTheSecondWindow()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);
            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp ClickIf");

            FormAutomation.ClickButton(hwnd, "btnVerDetalle");

            var recipe = new Recipe
            {
                Name = "Close details when visible",
                Steps =
                {
                    new RecipeStep
                    {
                        StepType = RecipeStepType.ClickIfWindowVisible,
                        Target = "Details",
                        Value = "btnCerrarDetalle",
                        SortOrder = 0
                    }
                }
            };

            RecipeRunner.RunRecipe(hwnd, template, recipe, new Dictionary<string, string>());

            var gone = Assert.Throws<InvalidOperationException>(() =>
                RecipeRunner.WaitForWindowByTitle(hwnd, "Details", TimeSpan.FromMilliseconds(1500)));
            Assert.Contains("Timed out waiting", gone.Message);
        }
        finally
        {
            Kill(process);
        }
    }

    private static FormField AssertField(FormTemplate template, string name, string controlType)
    {
        var field = template.Fields.FirstOrDefault(f => f.Name == name);
        Assert.NotNull(field);
        Assert.Equal(controlType, field!.ControlType);
        return field;
    }

    private static string? ReadField(FormTemplate template, IntPtr hwnd, string name)
    {
        return FormAutomation.ReadFieldValue(hwnd, template.Fields.First(f => f.Name == name));
    }

    private static ToggleState ReadCheckbox(IntPtr hwnd, string automationId)
    {
        using var automation = new UIA3Automation();
        using var application = Application.Attach(GetProcessId(hwnd));
        var root = automation.FromHandle(hwnd);
        var element = root!.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        return element!.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault;
    }

    private static string? ReadComboSelection(IntPtr hwnd, string automationId)
    {
        using var automation = new UIA3Automation();
        using var application = Application.Attach(GetProcessId(hwnd));
        var root = automation.FromHandle(hwnd);
        var combo = root!.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (combo?.Patterns.ExpandCollapse.IsSupported != true)
        {
            return null;
        }

        var expand = combo.Patterns.ExpandCollapse.Pattern;
        expand.Expand();
        try
        {
            var items = combo.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
            foreach (var item in items)
            {
                if (item.Patterns.SelectionItem.Pattern.IsSelected.ValueOrDefault)
                {
                    return item.Name;
                }
            }

            return null;
        }
        finally
        {
            try
            {
                expand.Collapse();
            }
            catch
            {
                // Collapsing after a failure is best-effort.
            }
        }
    }

    private static int GetProcessId(IntPtr hwnd)
    {
        _ = User32.GetWindowThreadProcessId(hwnd, out var processId);
        return (int)processId;
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
