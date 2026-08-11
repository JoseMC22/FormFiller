using FormFiller.Core.Licensing;

namespace FormFiller.Core.Tests;

public sealed class TrialServiceTests
{
    private static readonly DateTime FirstRun = new(2026, 1, 1, 9, 0, 0);

    [Fact]
    public void FirstRun_ReturnsTrialActive_WithFifteenDaysRemaining()
    {
        var service = new TrialService(NewStoragePath(), () => FirstRun);

        var status = service.Current;

        Assert.Equal(TrialState.TrialActive, status.State);
        Assert.Equal(15, status.RemainingDays);
        Assert.Equal(FirstRun, status.FirstRunAt);
        Assert.False(status.IsTampered);
    }

    [Fact]
    public void Day14_StillActive_RemainingDaysAreCorrect()
    {
        var storagePath = NewStoragePath();
        var firstDay = new TrialService(storagePath, () => FirstRun);
        _ = firstDay.Current;

        // Day 14 = 13 days after the install date.
        var service = new TrialService(storagePath, () => FirstRun.AddDays(13));
        var status = service.Current;

        Assert.Equal(TrialState.TrialActive, status.State);
        Assert.Equal(2, status.RemainingDays);
    }

    [Fact]
    public void Day15_LastActiveDay_HasOneDayRemaining()
    {
        var storagePath = NewStoragePath();
        _ = new TrialService(storagePath, () => FirstRun).Current;

        var service = new TrialService(storagePath, () => FirstRun.AddDays(14));
        var status = service.Current;

        Assert.Equal(TrialState.TrialActive, status.State);
        Assert.Equal(1, status.RemainingDays);
    }

    [Fact]
    public void Day16_TrialIsExpired()
    {
        var storagePath = NewStoragePath();
        _ = new TrialService(storagePath, () => FirstRun).Current;

        // Day 16 = 15 days after the install date: the trial window elapsed.
        var service = new TrialService(storagePath, () => FirstRun.AddDays(15));
        var status = service.Current;

        Assert.Equal(TrialState.Expired, status.State);
        Assert.Equal(0, status.RemainingDays);
    }

    [Fact]
    public void ClockRollback_LocksAsTampered()
    {
        var storagePath = NewStoragePath();
        var service = new TrialService(storagePath, () => FirstRun);
        _ = service.Current;

        Assert.Equal(
            TrialState.TrialActive,
            new TrialService(storagePath, () => FirstRun.AddDays(3)).Current.State);

        var rolledBack = new TrialService(storagePath, () => FirstRun.AddDays(1));
        var status = rolledBack.Current;

        Assert.Equal(TrialState.Tampered, status.State);
        Assert.True(status.IsTampered);
    }

    [Fact]
    public void TamperedFlag_SurvivesServiceRecreation()
    {
        var storagePath = NewStoragePath();
        var service = new TrialService(storagePath, () => FirstRun);
        _ = service.Current;
        _ = new TrialService(storagePath, () => FirstRun.AddDays(2)).Current;
        _ = new TrialService(storagePath, () => FirstRun.AddDays(1)).Current;

        var reloaded = new TrialService(storagePath, () => FirstRun.AddDays(2));

        Assert.Equal(TrialState.Tampered, reloaded.Current.State);
    }

    [Fact]
    public void Persistence_StateSurvivesServiceRecreation()
    {
        var storagePath = NewStoragePath();
        _ = new TrialService(storagePath, () => FirstRun).Current;

        var service = new TrialService(storagePath, () => FirstRun.AddDays(3));
        var status = service.Current;

        Assert.Equal(TrialState.TrialActive, status.State);
        Assert.Equal(12, status.RemainingDays);
        Assert.Equal(FirstRun, status.FirstRunAt);
    }

    [Fact]
    public void CorruptStorageFile_FailsLockedAsTampered()
    {
        var storagePath = NewStoragePath();
        var directory = Path.GetDirectoryName(storagePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(storagePath, "{ not valid json }");

        var service = new TrialService(storagePath, () => FirstRun);
        var status = service.Current;

        Assert.Equal(TrialState.Tampered, status.State);
        Assert.True(status.IsTampered);
    }

    private static string NewStoragePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "formfiller-trial-tests",
            $"{Guid.NewGuid():N}.json");
    }
}
