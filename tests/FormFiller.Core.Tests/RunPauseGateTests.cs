using FormFiller.Core.Automation;

namespace FormFiller.Core.Tests;

public sealed class RunPauseGateTests
{
    [Fact]
    public void Wait_ReturnsImmediately_WhenNotPaused()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var gate = new RunPauseGate();

        gate.Wait(cts.Token);
    }

    [Fact]
    public void IsPaused_ReflectsGateState()
    {
        var gate = new RunPauseGate();

        Assert.False(gate.IsPaused);

        gate.Pause();
        Assert.True(gate.IsPaused);

        gate.Resume();
        Assert.False(gate.IsPaused);
    }

    [Fact]
    public void Pause_IsIdempotent_UntilResume()
    {
        var gate = new RunPauseGate();

        gate.Pause();
        gate.Pause();

        Assert.True(gate.IsPaused);

        gate.Resume();
        Assert.False(gate.IsPaused);
    }

    [Fact]
    public async Task Wait_Blocks_UntilResume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var gate = new RunPauseGate();
        gate.Pause();

        var waiter = Task.Run(() => gate.Wait(cts.Token));
        var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromMilliseconds(200)));
        Assert.NotSame(waiter, completed);

        gate.Resume();

        await waiter;
    }

    [Fact]
    public async Task Wait_ThrowsOperationCanceledException_WhenCancelledWhilePaused()
    {
        using var cts = new CancellationTokenSource();
        var gate = new RunPauseGate();
        gate.Pause();

        var waiter = Task.Run(() => gate.Wait(cts.Token));
        var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromMilliseconds(200)));
        Assert.NotSame(waiter, completed);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
    }

    [Fact]
    public async Task Wait_Unblocks_WhenPauseThenResumeCycles()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var gate = new RunPauseGate();

        for (var cycle = 0; cycle < 3; cycle++)
        {
            gate.Pause();
            var waiter = Task.Run(() => gate.Wait(cts.Token));
            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromMilliseconds(200)));
            Assert.NotSame(waiter, completed);
            gate.Resume();
            await waiter;
        }
    }
}
