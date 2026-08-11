namespace FormFiller.Core.Automation;

/// <summary>
/// A thread-safe, cancellation-aware pause gate used to suspend a run between rows.
/// While the gate is paused, <see cref="Wait"/> blocks the calling thread until
/// <see cref="Resume"/> is called or the provided <see cref="CancellationToken"/> fires.
/// </summary>
public sealed class RunPauseGate
{
    private readonly object _sync = new();
    private TaskCompletionSource _openSignal = CreateCompletedSignal();

    private static TaskCompletionSource CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();
        return signal;
    }

    private static TaskCompletionSource CreateBlockedSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets whether the gate is currently blocking <see cref="Wait"/> callers.</summary>
    public bool IsPaused
    {
        get
        {
            lock (_sync)
            {
                return !_openSignal.Task.IsCompleted;
            }
        }
    }

    /// <summary>
    /// Closes the gate so subsequent <see cref="Wait"/> calls block until
    /// <see cref="Resume"/> is called. Pausing an already-paused gate is a no-op.
    /// </summary>
    public void Pause()
    {
        lock (_sync)
        {
            if (!_openSignal.Task.IsCompleted)
            {
                return;
            }

            _openSignal = CreateBlockedSignal();
        }
    }

    /// <summary>Opens the gate, releasing any blocked <see cref="Wait"/> callers.</summary>
    public void Resume()
    {
        lock (_sync)
        {
            _openSignal.TrySetResult();
        }
    }

    /// <summary>
    /// Blocks until the gate is open or <paramref name="ct"/> is cancelled.
    /// Returns immediately when the gate is not paused.
    /// </summary>
    /// <exception cref="OperationCanceledException">The token fired while waiting.</exception>
    public void Wait(CancellationToken ct)
    {
        Task gateTask;
        lock (_sync)
        {
            gateTask = _openSignal.Task;
        }

        if (gateTask.IsCompleted)
        {
            ct.ThrowIfCancellationRequested();
            return;
        }

        gateTask.WaitAsync(ct).GetAwaiter().GetResult();
    }
}
