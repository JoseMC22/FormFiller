using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FormFiller.Core.Models;

namespace FormFiller.Core.Automation;

public sealed record ControlNode(string Name, string AutomationId, string ControlType, string Path);

public static class UiInspector
{
    private const string CurrentAppProcessName = "FormFiller.App";

    public static IReadOnlyList<ProcessWindowInfo> GetOpenWindows()
    {
        var currentProcessName = Process.GetCurrentProcess().ProcessName;
        var seenHandles = new HashSet<IntPtr>();
        var seenProcessTitle = new HashSet<string>();
        var windows = new List<ProcessWindowInfo>();

        foreach (var process in Process.GetProcesses().OrderBy(p => p.ProcessName))
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                if (string.Equals(process.ProcessName, currentProcessName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(process.ProcessName, CurrentAppProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seenHandles.Add(process.MainWindowHandle))
                {
                    continue;
                }

                var processTitleKey = $"{process.ProcessName}|{process.MainWindowTitle}";
                if (process.MainWindowTitle.Length > 0 && !seenProcessTitle.Add(processTitleKey))
                {
                    continue;
                }

                windows.Add(new ProcessWindowInfo(process.Id, process.ProcessName, process.MainWindowTitle, process.MainWindowHandle));
            }
            catch
            {
                // Process may have exited while enumerating.
            }
        }

        return windows;
    }

    public static IReadOnlyList<ControlNode> GetControlTree(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return Array.Empty<ControlNode>();
        }

        using var automation = new UIA3Automation();
        var root = automation.FromHandle(hwnd);
        if (root == null)
        {
            return Array.Empty<ControlNode>();
        }

        var nodes = new List<ControlNode>();
        WalkControlTree(root, root.Name, nodes);
        return nodes;
    }

    public static FormTemplate CaptureWindow(IntPtr hwnd, string templateName)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        var processId = GetWindowProcessId(hwnd);
        var processName = GetProcessName(processId);
        var fields = new List<FormField>();
        var windowTitle = string.Empty;

        using (var application = Application.Attach(processId))
        using (var automation = new UIA3Automation())
        {
            var root = automation.FromHandle(hwnd);
            windowTitle = root?.Name ?? string.Empty;

            if (root != null)
            {
                var typeCounters = new Dictionary<string, int>();
                foreach (var child in root.FindAll(TreeScope.Children, TrueCondition.Default))
                {
                    WalkForFields(child, typeCounters, fields);
                }
            }
        }

        var sortedFields = fields
            .OrderBy(f => f.PositionY ?? int.MaxValue)
            .ThenBy(f => f.PositionX ?? int.MaxValue)
            .ToList();

        for (var i = 0; i < sortedFields.Count; i++)
        {
            sortedFields[i].SortOrder = i;
        }

        return new FormTemplate
        {
            Name = templateName,
            ProcessName = processName,
            WindowTitle = windowTitle,
            Fields = sortedFields
        };
    }

    private static void WalkControlTree(AutomationElement element, string path, List<ControlNode> nodes)
    {
        try
        {
            var controlType = element.ControlType.ToString();
            nodes.Add(new ControlNode(
                element.Name ?? string.Empty,
                element.AutomationId ?? string.Empty,
                controlType,
                path));

            foreach (var child in element.FindAll(TreeScope.Children, TrueCondition.Default))
            {
                var label = ChildLabel(child);
                var childPath = string.IsNullOrEmpty(path) ? label : $"{path}/{label}";
                WalkControlTree(child, childPath, nodes);
            }
        }
        catch
        {
            // Element went stale while walking the tree; skip it.
        }
    }

    private static void WalkForFields(AutomationElement element, Dictionary<string, int> typeCounters, List<FormField> fields)
    {
        try
        {
            var controlType = element.ControlType.ToString();
            var (fieldType, isInvokable) = ClassifyField(element, controlType);

            var counterIndex = typeCounters.GetValueOrDefault(controlType, 0);
            typeCounters[controlType] = counterIndex + 1;

            var name = NonEmpty(element.Name, element.AutomationId);
            var automationId = element.AutomationId;
            var rectangle = element.BoundingRectangle;

            fields.Add(new FormField
            {
                Name = string.IsNullOrWhiteSpace(name) ? $"{controlType}_{counterIndex}" : name,
                AutomationId = string.IsNullOrWhiteSpace(automationId) ? null : automationId,
                ControlType = controlType,
                FieldType = fieldType,
                IsEditable = IsEditable(element, fieldType),
                IsInvokable = isInvokable,
                PositionX = rectangle.Width > 0 ? rectangle.X + (rectangle.Width / 2) : null,
                PositionY = rectangle.Height > 0 ? rectangle.Y + (rectangle.Height / 2) : null
            });

            foreach (var child in element.FindAll(TreeScope.Children, TrueCondition.Default))
            {
                WalkForFields(child, typeCounters, fields);
            }
        }
        catch
        {
            // Element went stale while walking the tree; skip it.
        }
    }

    private static (FieldType Type, bool IsInvokable) ClassifyField(AutomationElement element, string controlType)
    {
        switch (controlType)
        {
            case "Edit":
                return (FieldType.Text, false);
            case "ComboBox":
                return (FieldType.ComboBox, false);
            case "CheckBox":
                return (FieldType.CheckBox, false);
            case "Button":
                var isInvokable = false;
                try
                {
                    isInvokable = element.Patterns.Invoke.IsSupported;
                }
                catch
                {
                    // Invoke pattern unavailable.
                }
                return (FieldType.Button, isInvokable);
            default:
                return (FieldType.Other, false);
        }
    }

    private static bool IsEditable(AutomationElement element, FieldType fieldType)
    {
        if (fieldType is not (FieldType.Text or FieldType.ComboBox))
        {
            return false;
        }

        try
        {
            if (element.Patterns.Value.IsSupported)
            {
                var valuePattern = element.Patterns.Value.Pattern;
                return !valuePattern.IsReadOnly.ValueOrDefault;
            }
        }
        catch
        {
            // Fall through to optimistic default.
        }

        return true;
    }

    private static string ChildLabel(AutomationElement child)
    {
        var name = NonEmpty(child.Name, child.AutomationId);
        return string.IsNullOrWhiteSpace(name) ? child.ControlType.ToString() : name;
    }

    private static string NonEmpty(string? first, string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        return second ?? string.Empty;
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

    private static string GetProcessName(int processId)
    {
        using var process = Process.GetProcessById(processId);
        return process.ProcessName;
    }
}
