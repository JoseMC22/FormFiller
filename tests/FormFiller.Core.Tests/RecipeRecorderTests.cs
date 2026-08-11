using System.Runtime.InteropServices;
using FormFiller.Core.Automation;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

public sealed class StepTranslationTests
{
    [Fact]
    public void ToStep_ValuePatternControl_ReturnsSetFieldWithAutomationIdTarget()
    {
        var control = new CapturedControl("Codigo", "txtCodigo", "Edit", true, false);

        var step = StepTranslation.ToStep(control, 0);

        Assert.NotNull(step);
        Assert.Equal(RecipeStepType.SetField, step!.StepType);
        Assert.Equal("txtCodigo", step.Target);
        Assert.Equal(0, step.SortOrder);
        Assert.Null(step.Value);
    }

    [Fact]
    public void ToStep_ValuePatternControlWithoutAutomationId_FallsBackToName()
    {
        var control = new CapturedControl("Codigo", string.Empty, "Edit", true, false);

        var step = StepTranslation.ToStep(control, 3);

        Assert.NotNull(step);
        Assert.Equal(RecipeStepType.SetField, step!.StepType);
        Assert.Equal("Codigo", step.Target);
        Assert.Equal(3, step.SortOrder);
    }

    [Fact]
    public void ToStep_InvokePatternControl_ReturnsClickButtonWithAutomationIdTarget()
    {
        var control = new CapturedControl("Guardar", "btnGuardar", "Button", false, true);

        var step = StepTranslation.ToStep(control, 5);

        Assert.NotNull(step);
        Assert.Equal(RecipeStepType.ClickButton, step!.StepType);
        Assert.Equal("btnGuardar", step.Target);
        Assert.Equal(5, step.SortOrder);
    }

    [Fact]
    public void ToStep_ControlWithNoNameOrAutomationId_ReturnsNull()
    {
        var control = new CapturedControl(string.Empty, string.Empty, "Edit", true, false);

        var step = StepTranslation.ToStep(control, 0);

        Assert.Null(step);
    }

    [Fact]
    public void ToStep_ControlWithNoSupportedPatterns_ReturnsNull()
    {
        var control = new CapturedControl("Estado", "lblEstado", "Text", false, false);

        var step = StepTranslation.ToStep(control, 0);

        Assert.Null(step);
    }

    [Fact]
    public void ToStep_ControlSupportingBothPatterns_PrefersSetField()
    {
        var control = new CapturedControl("Nombre", "txtNombre", "Edit", true, true);

        var step = StepTranslation.ToStep(control, 0);

        Assert.NotNull(step);
        Assert.Equal(RecipeStepType.SetField, step!.StepType);
    }

    [Fact]
    public void ToStep_ForcedInvokeOnControlSupportingBothPatterns_ReturnsClickButton()
    {
        var control = new CapturedControl("Guardar", "btnGuardar", "Button", true, true);

        var step = StepTranslation.ToStep(control, 0, forceInvoke: true);

        Assert.NotNull(step);
        Assert.Equal(RecipeStepType.ClickButton, step!.StepType);
        Assert.Equal("btnGuardar", step.Target);
    }
}

[Collection("MuestraApp fixture")]
public sealed class RecipeRecorderTests
{
    [Fact]
    public void StartRecording_CapturesFieldsAndClick_WhenMuestraAppIsDriven()
    {
        using var process = MuestraAppFixture.Start();
        try
        {
            process.WaitForInputIdle(5000);

            var hwnd = MuestraAppFixture.WaitForMainWindowHandle(process, TimeSpan.FromSeconds(5));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            var template = UiInspector.CaptureWindow(hwnd, "MuestraApp Recorder");
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.Name == "Codigo"));
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.Name == "Nombre"));
            Assert.NotNull(template.Fields.FirstOrDefault(f => f.Name == "Direccion"));

            using var pump = new RecorderPump(hwnd);
            Assert.True(pump.Recorder!.IsRecording);

            FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
            {
                ["Codigo"] = "REC-001"
            });

            // Back-to-back edits to the same control must collapse into a single SetField step.
            FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
            {
                ["Codigo"] = "REC-002"
            });

            FormAutomation.FillFields(hwnd, template, new Dictionary<string, string>
            {
                ["Nombre"] = "Registrar",
                ["Direccion"] = "Calle 1"
            });

            FormAutomation.ClickButton(hwnd, "btnGuardar");

            var recorded = WaitForRecordedSteps(pump, TimeSpan.FromSeconds(10));
            var steps = recorded.ToList();

            var codigoStep = Assert.Single(steps, s => IsSetField("txtCodigo")(s));
            Assert.NotNull(Assert.Single(steps, s => IsSetField("txtNombre")(s)));
            Assert.NotNull(Assert.Single(steps, s => IsSetField("txtDireccion")(s)));
            var clickStep = Assert.Single(steps, s => IsClickButton("btnGuardar")(s));

            Assert.True(
                steps.IndexOf(clickStep) > steps.IndexOf(codigoStep),
                "The ClickButton step for Guardar should come after the SetField steps.");
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

    private static Func<RecipeStep, bool> IsSetField(string target) =>
        step => step.StepType == RecipeStepType.SetField
            && string.Equals(step.Target, target, StringComparison.OrdinalIgnoreCase);

    private static Func<RecipeStep, bool> IsClickButton(string target) =>
        step => step.StepType == RecipeStepType.ClickButton
            && string.Equals(step.Target, target, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<RecipeStep> WaitForRecordedSteps(RecorderPump pump, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var steps = pump.Steps;
            if (steps.Any(IsSetField("txtCodigo"))
                && steps.Any(IsSetField("txtNombre"))
                && steps.Any(IsSetField("txtDireccion"))
                && steps.Any(IsClickButton("btnGuardar")))
            {
                return steps;
            }

            Thread.Sleep(50);
        }

        return pump.Steps;
    }

    /// <summary>
    /// Hosts the <see cref="RecipeRecorder"/> on a dedicated STA thread that pumps its own
    /// message queue, because UI Automation events are only delivered to the thread that
    /// registered the handlers and that thread must pump messages.
    /// </summary>
    private sealed class RecorderPump : IDisposable
    {
        private const uint WmQuit = 0x0012;

        private readonly Thread _thread;
        private readonly ManualResetEventSlim _ready = new(false);
        private int _osThreadId;
        private volatile IReadOnlyList<RecipeStep> _finalSteps = Array.Empty<RecipeStep>();

        public RecorderPump(IntPtr hwnd)
        {
            _thread = new Thread(() => Run(hwnd))
            {
                IsBackground = true,
                Name = "Recorder UIA pump"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();

            if (!_ready.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("The recorder pump did not start in time.");
            }

            if (StartupError is not null)
            {
                throw new InvalidOperationException("The recorder pump failed to start.", StartupError);
            }
        }

        public RecipeRecorder? Recorder { get; private set; }

        public Exception? StartupError { get; private set; }

        public IReadOnlyList<RecipeStep> Steps
        {
            get
            {
                var recorder = Recorder;
                return recorder is not null ? recorder.RecordedSteps : _finalSteps;
            }
        }

        public void Stop()
        {
            if (Recorder is not null)
            {
                PostThreadMessage(_osThreadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            }

            if (!_thread.Join(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The recorder pump did not stop in time.");
            }
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch
            {
                // Best-effort pump shutdown.
            }

            _ready.Dispose();
        }

        private void Run(IntPtr hwnd)
        {
            try
            {
                _osThreadId = GetCurrentThreadId();
                var recorder = new RecipeRecorder();
                recorder.StartRecording(hwnd);
                Recorder = recorder;
                _ready.Set();

                while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref message);
                    DispatchMessage(ref message);
                }
            }
            catch (Exception ex)
            {
                StartupError = ex;
                _ready.Set();
            }
            finally
            {
                if (Recorder is not null)
                {
                    _finalSteps = Recorder.RecordedSteps;
                    Recorder.Dispose();
                    Recorder = null;
                }
            }
        }

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostThreadMessage(int threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Hwnd;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public NativePoint Point;
        }
    }
}
