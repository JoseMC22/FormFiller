namespace FormFiller.Core.Licensing;

/// <summary>
/// The lifecycle state of the local trial license.
/// </summary>
public enum TrialState
{
    /// <summary>The trial has never been started on this machine.</summary>
    NotStarted = 0,

    /// <summary>The trial is running and the app may be used.</summary>
    TrialActive = 1,

    /// <summary>The trial window elapsed and the app must be blocked.</summary>
    Expired = 2,

    /// <summary>A clock rollback was detected; the app must be blocked.</summary>
    Tampered = 3
}

/// <summary>
/// The result of evaluating the local trial at a given point in time.
/// </summary>
public sealed record TrialStatus(
    TrialState State,
    int? RemainingDays,
    DateTime? FirstRunAt,
    DateTime LastSeenAt,
    bool IsTampered);
