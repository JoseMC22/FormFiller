using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FormFiller.Core.Models;

namespace FormFiller.Core.Automation;

/// <summary>
/// A read-only snapshot of a control captured from a UI Automation focus or pattern event.
/// </summary>
public sealed record CapturedControl(
    string Name,
    string AutomationId,
    string ControlType,
    bool SupportsValuePattern,
    bool SupportsInvokePattern,
    bool SupportsSelectionItem = false,
    bool SupportsToggle = false);

/// <summary>
/// Translates a captured control into a <see cref="RecipeStep"/> using the existing step vocabulary.
///
/// Translation rules:
/// - A control that supports ValuePattern, SelectionItemPattern or TogglePattern becomes a SetField
///   step (Target = AutomationId or Name). These patterns mark a value-bearing control: edit boxes,
///   combo boxes, checkboxes, radios and date pickers are all set-fields, not buttons.
/// - A control that supports InvokePattern becomes a ClickButton step (Target = AutomationId or Name).
/// - A value-bearing control wins over InvokePattern unless the control was explicitly invoked
///   (forceInvoke), because a single control can expose both sets of patterns.
/// - Controls without a usable name or automation id, or without any of the recognized patterns,
///   are skipped.
/// </summary>
public static class StepTranslation
{
    public static RecipeStep? ToStep(CapturedControl control, int sortOrder, bool forceInvoke = false)
    {
        ArgumentNullException.ThrowIfNull(control);

        var target = !string.IsNullOrWhiteSpace(control.AutomationId) ? control.AutomationId : control.Name;
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        if (control.SupportsInvokePattern && forceInvoke)
        {
            return ClickButton(target, sortOrder);
        }

        if (control.SupportsValuePattern || control.SupportsSelectionItem || control.SupportsToggle)
        {
            return SetField(target, sortOrder);
        }

        if (control.SupportsInvokePattern)
        {
            return ClickButton(target, sortOrder);
        }

        return null;
    }

    private static RecipeStep SetField(string target, int sortOrder) => new()
    {
        StepType = RecipeStepType.SetField,
        Target = target,
        SortOrder = sortOrder
    };

    private static RecipeStep ClickButton(string target, int sortOrder) => new()
    {
        StepType = RecipeStepType.ClickButton,
        Target = target,
        SortOrder = sortOrder
    };
}

/// <summary>
/// Records a user's manual interactions on a target window into <see cref="RecipeStep"/>s.
///
/// The recorder subscribes to three UI Automation event sources and folds them into the
/// recipe step vocabulary:
/// - FocusChanged: captures which control the user focused, so clicking into a field or
///   a button is recorded even when no value changes and no invoke event is raised.
/// - Value property changed: captures edits to editable controls (SetField).
/// - Invoke: captures button activations (ClickButton).
///
/// Radios, checkboxes and date pickers surface SelectionItemPattern/TogglePattern and are
/// translated to SetField steps, so focusing them records a set-field just like a text box.
///
/// Steps are deduplicated so that consecutive SetField steps for the same control (and the
/// same for ClickButton) collapse into a single step. The recorded Value of a SetField step
/// stays null because the runner interprets it as an Excel column name; the user maps the
/// column later, either in the Recorder grid or the Recipes tab.
///
/// The recorder only captures interactions that belong to the target window's process, so
/// interacting with other applications (including the host app itself) is ignored.
/// </summary>
public sealed class RecipeRecorder : IDisposable
{
    private readonly object _lock = new();
    private readonly List<RecipeStep> _steps = new();

    private UIA3Automation? _automation;
    private AutomationElement? _root;
    private FocusChangedEventHandlerBase? _focusChangedHandler;
    private AutomationEventHandlerBase? _invokedHandler;
    private PropertyChangedEventHandlerBase? _valueChangedHandler;
    private int _targetProcessId;
    private bool _disposed;

    public bool IsRecording { get; private set; }

    public IntPtr TargetWindowHandle { get; private set; }

    /// <summary>Raised on the thread that receives the UI Automation event (the UI thread in the app).</summary>
    public event Action<RecipeStep>? StepRecorded;

    /// <summary>A thread-safe snapshot of the steps recorded so far.</summary>
    public IReadOnlyList<RecipeStep> RecordedSteps
    {
        get
        {
            lock (_lock)
            {
                return _steps.ToList();
            }
        }
    }

    /// <summary>
    /// Attaches to the window identified by <paramref name="hwnd"/> and starts listening for
    /// UI Automation events. Must be called from a thread that runs a message pump (in the WPF
    /// app this is the UI thread) so events can be delivered.
    /// </summary>
    /// <exception cref="InvalidOperationException">The window cannot be reached through UI Automation.</exception>
    public void StartRecording(IntPtr hwnd)
    {
        ThrowIfDisposed();
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("The window handle is invalid.", nameof(hwnd));
        }

        if (IsRecording)
        {
            throw new InvalidOperationException("The recorder is already recording.");
        }

        var processId = GetWindowProcessId(hwnd);
        var automation = new UIA3Automation();
        var root = automation.FromHandle(hwnd);
        if (root is null)
        {
            automation.Dispose();
            throw new InvalidOperationException("Unable to reach the target window through UI Automation.");
        }

        try
        {
            _automation = automation;
            _root = root;
            _targetProcessId = processId;
            TargetWindowHandle = hwnd;

            _focusChangedHandler = automation.RegisterFocusChangedEvent(OnFocusChanged);
            _invokedHandler = root.RegisterAutomationEvent(
                automation.EventLibrary.Invoke.InvokedEvent,
                TreeScope.Descendants,
                OnInvoked);
            _valueChangedHandler = root.RegisterPropertyChangedEvent(
                TreeScope.Descendants,
                OnPropertyChanged,
                new[] { automation.PropertyLibrary.Value.Value });

            IsRecording = true;
        }
        catch
        {
            UnsubscribeAll();
            throw;
        }
    }

    /// <summary>
    /// Unsubscribes all event handlers and returns a snapshot of the recorded steps.
    /// </summary>
    public IReadOnlyList<RecipeStep> StopRecording()
    {
        if (!IsRecording)
        {
            return RecordedSteps;
        }

        UnsubscribeAll();
        IsRecording = false;
        TargetWindowHandle = IntPtr.Zero;
        return RecordedSteps;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopRecording();
        UnsubscribeAll();
    }

    private void OnFocusChanged(AutomationElement element)
    {
        TryRecordFromElement(element, forceInvoke: false);
    }

    private void OnInvoked(AutomationElement element, EventId eventId)
    {
        TryRecordFromElement(element, forceInvoke: true);
    }

    private void OnPropertyChanged(AutomationElement element, PropertyId propertyId, object? newValue)
    {
        if (newValue is string text && string.IsNullOrWhiteSpace(text))
        {
            // A programmatic clear (for example after a form reset) is not user input intent.
            return;
        }

        TryRecordFromElement(element, forceInvoke: false);
    }

    private void TryRecordFromElement(AutomationElement element, bool forceInvoke)
    {
        if (!IsRecording || _targetProcessId <= 0 || !BelongsToTargetProcess(element))
        {
            return;
        }

        var control = CaptureControl(element);
        if (control is null)
        {
            return;
        }

        RecipeStep? step;
        lock (_lock)
        {
            step = StepTranslation.ToStep(control, _steps.Count, forceInvoke);
            if (step is null || IsDuplicate(step))
            {
                return;
            }

            _steps.Add(step);
        }

        StepRecorded?.Invoke(step);
    }

    private bool BelongsToTargetProcess(AutomationElement element)
    {
        try
        {
            return element.Properties.ProcessId.ValueOrDefault == _targetProcessId;
        }
        catch
        {
            return false;
        }
    }

    private static CapturedControl? CaptureControl(AutomationElement element)
    {
        var name = ReadString(element.Properties.Name);
        var automationId = ReadString(element.Properties.AutomationId);
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        var controlType = SafeRead(() => element.ControlType.ToString(), string.Empty);
        var supportsValue = SafeRead(() => element.Patterns.Value.IsSupported, false);
        var supportsInvoke = SafeRead(() => element.Patterns.Invoke.IsSupported, false);
        var supportsSelectionItem = SafeRead(() => element.Patterns.SelectionItem.IsSupported, false);
        var supportsToggle = SafeRead(() => element.Patterns.Toggle.IsSupported, false);

        return new CapturedControl(
            name,
            automationId,
            controlType,
            supportsValue,
            supportsInvoke,
            supportsSelectionItem,
            supportsToggle);
    }

    private static string ReadString(AutomationProperty<string> property)
    {
        try
        {
            return property.ValueOrDefault ?? string.Empty;
        }
        catch
        {
            // The property may be unsupported or the element went stale.
            return string.Empty;
        }
    }

    private static T SafeRead<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }

    private bool IsDuplicate(RecipeStep step)
    {
        if (_steps.Count == 0)
        {
            return false;
        }

        var last = _steps[^1];
        return last.StepType == step.StepType
            && string.Equals(last.Target, step.Target, StringComparison.OrdinalIgnoreCase);
    }

    private void UnsubscribeAll()
    {
        try
        {
            _focusChangedHandler?.Dispose();
        }
        catch
        {
            // The handler may already be released.
        }

        try
        {
            _invokedHandler?.Dispose();
        }
        catch
        {
            // The handler may already be released.
        }

        try
        {
            _valueChangedHandler?.Dispose();
        }
        catch
        {
            // The handler may already be released.
        }

        _focusChangedHandler = null;
        _invokedHandler = null;
        _valueChangedHandler = null;
        _root = null;

        var automation = _automation;
        _automation = null;
        if (automation is null)
        {
            return;
        }

        try
        {
            automation.UnregisterAllEvents();
        }
        catch
        {
            // Event handlers may already be gone.
        }

        try
        {
            automation.Dispose();
        }
        catch
        {
            // The COM automation object may already be released.
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RecipeRecorder));
        }
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
