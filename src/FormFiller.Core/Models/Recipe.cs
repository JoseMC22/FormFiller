namespace FormFiller.Core.Models;

/// <summary>
/// The kinds of steps a recipe can perform against the target form window.
/// </summary>
public enum RecipeStepType
{
    SetField = 0,
    ClickButton = 1,
    Wait = 2,
    WaitForWindow = 3,
    ClickIfWindowVisible = 4
}

/// <summary>
/// A single step inside a <see cref="Recipe"/>.
///
/// Step semantics:
/// - SetField: Target = the form field name, Value = the Excel column to read the value from.
/// - ClickButton: Target = the button name or AutomationId to click.
/// - Wait: Value = the number of milliseconds to sleep.
/// - WaitForWindow: Target = the window title substring to wait for, Value = the timeout in milliseconds.
/// - ClickIfWindowVisible: Target = the window title substring to probe (1500 ms window),
///   Value = the button to click when the window appears; it never fails when the window is absent.
/// </summary>
public class RecipeStep
{
    public int Id { get; set; }

    public int RecipeId { get; set; }

    public RecipeStepType StepType { get; set; }

    public string? Target { get; set; }

    public string? Value { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// A named, ordered sequence of <see cref="RecipeStep"/>s bound to a template.
/// </summary>
public class Recipe
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RecipeStep> Steps { get; set; } = new();
}
