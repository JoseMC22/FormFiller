using System.Text.Json;

namespace FormFiller.Core.Licensing;

/// <summary>
/// A simple, honest local trial gate: the app is usable for 15 days from first
/// run, then blocks. The clock is injectable so tests can simulate any date and
/// a clock rollback without waiting real time.
///
/// Storage is a small JSON file under %APPDATA%\FormFiller. A plain file was
/// chosen over the SQLite AppDb because the state is a single tiny blob that
/// must be readable at startup before any repository is touched, needs no
/// schema migration surface, and must stay independent from user data so a
/// corrupted database can never silently restart the trial. This is a local
/// trial, so no encryption is applied; the real license server replaces this
/// later.
/// </summary>
public sealed class TrialService
{
    public const string SkipTrialEnvironmentVariable = "FORMFILLER_SKIP_TRIAL";

    public static readonly TimeSpan TrialLength = TimeSpan.FromDays(15);

    private readonly string _storagePath;
    private readonly Func<DateTime> _clock;

    public TrialService(string? storagePath = null, Func<DateTime>? clock = null)
    {
        _storagePath = Path.GetFullPath(storagePath ?? DefaultStoragePath);
        _clock = clock ?? (() => DateTime.Now);
    }

    /// <summary>The default trial state file location under %APPDATA%\FormFiller.</summary>
    public static string DefaultStoragePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FormFiller",
            "trial.json");

    /// <summary>
    /// True when the <see cref="SkipTrialEnvironmentVariable"/> escape hatch is
    /// set to "1". Used by development and tests to bypass the trial gate.
    /// </summary>
    public static bool IsTrialGateDisabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(SkipTrialEnvironmentVariable),
            "1",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Evaluates the trial against the injected clock.</summary>
    public TrialStatus Current => Evaluate(_clock());

    /// <summary>
    /// Evaluates the trial as of <paramref name="now"/> and persists any state
    /// transition. A clock rollback relative to the last seen date locks the
    /// trial into <see cref="TrialState.Tampered"/>.
    /// </summary>
    public TrialStatus Evaluate(DateTime now)
    {
        var data = Load();

        if (data.IsTampered)
        {
            return ToStatus(TrialState.Tampered, 0, data);
        }

        if (data.FirstRunAt is null || data.LastSeenAt is null)
        {
            data.FirstRunAt = now;
            data.LastSeenAt = now;
            Save(data);
            return ToStatus(TrialState.TrialActive, TrialLength.Days, data);
        }

        var firstRunDate = data.FirstRunAt.Value.Date;
        var lastSeenDate = data.LastSeenAt.Value.Date;
        var today = now.Date;

        if (today < lastSeenDate)
        {
            data.IsTampered = true;
            Save(data);
            return ToStatus(TrialState.Tampered, 0, data);
        }

        if (today > lastSeenDate)
        {
            data.LastSeenAt = now;
            Save(data);
        }

        var elapsedDays = (today - firstRunDate).Days;
        if (elapsedDays >= TrialLength.Days)
        {
            return ToStatus(TrialState.Expired, 0, data);
        }

        return ToStatus(TrialState.TrialActive, TrialLength.Days - elapsedDays, data);
    }

    private static TrialStatus ToStatus(TrialState state, int remainingDays, TrialStorageData data)
    {
        return new TrialStatus(
            state,
            remainingDays,
            data.FirstRunAt,
            data.LastSeenAt ?? DateTime.MinValue,
            data.IsTampered);
    }

    private TrialStorageData Load()
    {
        if (!File.Exists(_storagePath))
        {
            return new TrialStorageData();
        }

        try
        {
            var json = File.ReadAllText(_storagePath);
            return JsonSerializer.Deserialize<TrialStorageData>(json) ?? new TrialStorageData();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A missing or corrupt trial file must not silently grant a fresh
            // trial; fail locked instead.
            return new TrialStorageData { IsTampered = true };
        }
    }

    private void Save(TrialStorageData data)
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = _storagePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _storagePath, overwrite: true);
    }
}

internal sealed class TrialStorageData
{
    public DateTime? FirstRunAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public bool IsTampered { get; set; }
}
