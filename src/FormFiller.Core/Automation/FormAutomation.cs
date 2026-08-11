using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FormFiller.Core.Models;

namespace FormFiller.Core.Automation;

public static class FormAutomation
{
    private static readonly string[] TruthyValues =
        { "1", "true", "si", "sí", "yes", "x", "on", "activo", "check" };

    public static void FillFields(IntPtr hwnd, FormTemplate template, IReadOnlyDictionary<string, string> values)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        var processId = GetWindowProcessId(hwnd);
        var errors = new List<string>();

        using (var application = Application.Attach(processId))
        using (var automation = new UIA3Automation())
        {
            var root = automation.FromHandle(hwnd);
            if (root is null)
            {
                throw new InvalidOperationException("Unable to reach the target window through UI Automation.");
            }

            foreach (var field in template.Fields)
            {
                if (!values.TryGetValue(field.Name, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                try
                {
                    var element = FindFieldElement(root, field);
                    if (element is null)
                    {
                        errors.Add($"'{field.Name}': element not found");
                        continue;
                    }

                    switch (field.FieldType)
                    {
                        case FieldType.Text:
                            SetTextValue(element, value);
                            break;
                        case FieldType.ComboBox:
                            SetComboBoxValue(element, value);
                            break;
                        case FieldType.CheckBox:
                            SetCheckBoxValue(element, value);
                            break;
                        case FieldType.RadioButton:
                            SetRadioButtonValue(element, value);
                            break;
                        case FieldType.DatePicker:
                            SetDatePickerValue(element, value);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"'{field.Name}': {ex.Message}");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Failed to fill the form:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    public static void ClickButton(IntPtr hwnd, string buttonName)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        if (string.IsNullOrWhiteSpace(buttonName))
        {
            throw new ArgumentException("The button name is required.", nameof(buttonName));
        }

        var processId = GetWindowProcessId(hwnd);
        using var application = Application.Attach(processId);
        using var automation = new UIA3Automation();
        var root = automation.FromHandle(hwnd);
        if (root is null)
        {
            throw new InvalidOperationException("Unable to reach the target window through UI Automation.");
        }

        var button = FindButton(root, buttonName);
        if (button is null)
        {
            throw new InvalidOperationException($"Button '{buttonName}' was not found.");
        }

        try
        {
            if (button.Patterns.Invoke.IsSupported)
            {
                button.Patterns.Invoke.Pattern.Invoke();
                return;
            }

            var clickablePoint = button.GetClickablePoint();
            Mouse.Click(clickablePoint, MouseButton.Left);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to click button '{buttonName}'.", ex);
        }
    }

    public static string? ReadFieldValue(IntPtr hwnd, FormField field)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        ArgumentNullException.ThrowIfNull(field);

        var processId = GetWindowProcessId(hwnd);
        using var application = Application.Attach(processId);
        using var automation = new UIA3Automation();
        var root = automation.FromHandle(hwnd);
        if (root is null)
        {
            return null;
        }

        try
        {
            var element = FindFieldElement(root, field);
            if (element is null || !element.Patterns.Value.IsSupported)
            {
                return null;
            }

            return element.Patterns.Value.Pattern.Value.ValueOrDefault;
        }
        catch
        {
            return null;
        }
    }

    private static void SetTextValue(AutomationElement element, string value)
    {
        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return;
        }

        if (element.Patterns.Text.IsSupported)
        {
            element.Focus();
            Keyboard.Type(value);
            return;
        }

        throw new InvalidOperationException("the element supports neither ValuePattern nor TextPattern");
    }

    private static void SetComboBoxValue(AutomationElement element, string value)
    {
        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return;
        }

        if (!element.Patterns.ExpandCollapse.IsSupported)
        {
            throw new InvalidOperationException("the element supports neither ValuePattern nor ExpandCollapsePattern");
        }

        var expandCollapse = element.Patterns.ExpandCollapse.Pattern;
        expandCollapse.Expand();
        try
        {
            var item = FindFirstMatching(element, e => Matches(NameOf(e), value));
            if (item is null)
            {
                throw new InvalidOperationException($"combo item '{value}' was not found");
            }

            if (!item.Patterns.SelectionItem.IsSupported)
            {
                throw new InvalidOperationException($"combo item '{value}' does not support SelectionItemPattern");
            }

            item.Patterns.SelectionItem.Pattern.Select();
        }
        finally
        {
            try
            {
                expandCollapse.Collapse();
            }
            catch
            {
                // Collapsing after a failure is best-effort.
            }
        }
    }

    private static void SetCheckBoxValue(AutomationElement element, string value)
    {
        if (!element.Patterns.Toggle.IsSupported)
        {
            throw new InvalidOperationException("the element does not support TogglePattern");
        }

        var target = IsTruthy(value) ? ToggleState.On : ToggleState.Off;
        var current = element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault;
        if (current != target)
        {
            element.Patterns.Toggle.Pattern.Toggle();
        }
    }

    private static void SetRadioButtonValue(AutomationElement element, string value)
    {
        // Radios can only be turned on through UI automation; a falsy/empty value is a no-op
        // because deselecting a radio safely is not possible across providers.
        if (!IsTruthy(value))
        {
            return;
        }

        if (element.Patterns.SelectionItem.IsSupported)
        {
            if (!element.Patterns.SelectionItem.Pattern.IsSelected.ValueOrDefault)
            {
                element.Patterns.SelectionItem.Pattern.Select();
            }

            return;
        }

        if (!element.Patterns.Toggle.IsSupported)
        {
            throw new InvalidOperationException("the element supports neither SelectionItemPattern nor TogglePattern");
        }

        if (element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault != ToggleState.On)
        {
            element.Patterns.Toggle.Pattern.Toggle();
        }
    }

    private static void SetDatePickerValue(AutomationElement element, string value)
    {
        // The WinForms DateTimePicker exposes a ValuePattern but its SetValue silently ignores
        // the request, so the reliable path is keyboard input: focus, select all, then type.
        element.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(value);
    }

    private static bool IsTruthy(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return TruthyValues.Contains(normalized, StringComparer.Ordinal);
    }

    private static AutomationElement? FindFieldElement(AutomationElement root, FormField field)
    {
        if (!string.IsNullOrWhiteSpace(field.AutomationId))
        {
            var byId = FindByAutomationId(root, field.AutomationId);
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(field.Name))
        {
            var byName = FindByName(root, field.Name);
            if (byName != null)
            {
                return byName;
            }
        }

        return null;
    }

    private static AutomationElement? FindButton(AutomationElement root, string buttonName)
    {
        var byName = FindByName(root, buttonName);
        if (byName != null)
        {
            return byName;
        }

        return FindByAutomationId(root, buttonName);
    }

    private static AutomationElement? FindByName(AutomationElement root, string name)
    {
        try
        {
            var element = root.FindFirstDescendant(cf => cf.ByName(name));
            if (element != null)
            {
                return element;
            }
        }
        catch
        {
            // Element went stale while searching; fall back to the manual scan.
        }

        return FindFirstMatching(root, e => Matches(NameOf(e), name));
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        try
        {
            var element = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (element != null)
            {
                return element;
            }
        }
        catch
        {
            // Element went stale while searching; fall back to the manual scan.
        }

        return FindFirstMatching(root, e => Matches(AutomationIdOf(e), automationId));
    }

    private static AutomationElement? FindFirstMatching(AutomationElement root, Func<AutomationElement, bool> predicate)
    {
        AutomationElement[] descendants;
        try
        {
            descendants = root.FindAllDescendants(TrueCondition.Default);
        }
        catch
        {
            return null;
        }

        foreach (var candidate in descendants)
        {
            try
            {
                if (predicate(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Element went stale; keep scanning.
            }
        }

        return null;
    }

    private static bool Matches(string? candidate, string? expected)
    {
        return !string.IsNullOrWhiteSpace(candidate)
            && !string.IsNullOrWhiteSpace(expected)
            && string.Equals(candidate.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NameOf(AutomationElement element) => element.Properties.Name.ValueOrDefault;

    private static string? AutomationIdOf(AutomationElement element) => element.Properties.AutomationId.ValueOrDefault;

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
