using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Provides pacing awareness for the current practice session.
/// Tracks session-level patterns (how long, how many today, rest gaps)
/// and produces non-judgmental observations for the UI.
///
/// This is the bridge between FatigueDetector (cross-session) and
/// the current moment. FatigueDetector looks at history;
/// SessionPacer looks at right now.
/// </summary>
public sealed class SessionPacer
{
    private readonly FatigueDetector _fatigueDetector = new();
    private DateTime? _sessionStartedAt;
    private int _sessionsThisLaunch;

    /// <summary>
    /// Marks the start of a new typing session.
    /// </summary>
    public void OnSessionStarted()
    {
        _sessionStartedAt = DateTime.UtcNow;
        _sessionsThisLaunch++;
    }

    /// <summary>
    /// Marks the end of a typing session.
    /// </summary>
    public void OnSessionCompleted()
    {
        _sessionStartedAt = null;
    }

    /// <summary>
    /// Marks the app was just opened (reset per-launch counters).
    /// </summary>
    public void OnAppLaunched()
    {
        _sessionsThisLaunch = 0;
        _sessionStartedAt = null;
    }

    /// <summary>
    /// Gets the number of sessions completed since the app was opened.
    /// </summary>
    public int SessionsThisLaunch => _sessionsThisLaunch;

    /// <summary>
    /// Produces a pacing snapshot for the current moment.
    /// </summary>
    public PacingSnapshot GetSnapshot(LongitudinalData data)
    {
        var cadence = _fatigueDetector.Observe(data);
        var timeSince = _fatigueDetector.TimeSinceLastSession(data);

        return new PacingSnapshot
        {
            SessionsThisLaunch = _sessionsThisLaunch,
            SessionsToday = cadence?.SessionsToday ?? 0,
            FatigueSignal = cadence?.Signal ?? FatigueSignal.Fresh,
            AccuracyDeclining = cadence?.AccuracyDeclining ?? false,
            TimeSinceLastSession = timeSince ?? "first session",
            IsInSession = _sessionStartedAt.HasValue,
            CurrentSessionDuration = _sessionStartedAt.HasValue
                ? DateTime.UtcNow - _sessionStartedAt.Value
                : TimeSpan.Zero,
            PaceLabel = ComputePaceLabel(
                cadence?.Signal ?? FatigueSignal.Fresh,
                _sessionsThisLaunch,
                cadence?.AccuracyDeclining ?? false)
        };
    }

    /// <summary>
    /// Computes a short, non-judgmental label describing the current pace.
    /// </summary>
    private static string ComputePaceLabel(
        FatigueSignal signal, int sessionsThisLaunch, bool accuracyDeclining)
    {
        if (sessionsThisLaunch == 0)
            return "Ready to start";

        return signal switch
        {
            FatigueSignal.Fresh => sessionsThisLaunch switch
            {
                1 => "Warming up",
                2 or 3 => "Good pace",
                _ => "Steady"
            },
            FatigueSignal.Steady => "Steady pace",
            FatigueSignal.ActivePace => accuracyDeclining
                ? "High pace"
                : "In the zone",
            FatigueSignal.HighIntensity => "Intense session",
            _ => "Practicing"
        };
    }
}

/// <summary>
/// A snapshot of the user's current practice pace.
/// All labels are observational, not judgmental.
/// </summary>
public sealed class PacingSnapshot
{
    /// <summary>Sessions completed since app opened.</summary>
    public int SessionsThisLaunch { get; init; }

    /// <summary>Sessions completed today (all launches).</summary>
    public int SessionsToday { get; init; }

    /// <summary>Current fatigue signal from longitudinal analysis.</summary>
    public FatigueSignal FatigueSignal { get; init; }

    /// <summary>Whether accuracy is trending down in recent sessions.</summary>
    public bool AccuracyDeclining { get; init; }

    /// <summary>Human-readable time since last completed session.</summary>
    public string TimeSinceLastSession { get; init; } = "";

    /// <summary>Whether a typing session is currently active.</summary>
    public bool IsInSession { get; init; }

    /// <summary>How long the current session has been running.</summary>
    public TimeSpan CurrentSessionDuration { get; init; }

    /// <summary>
    /// Short, non-judgmental label for the current pace.
    /// Examples: "Warming up", "Good pace", "In the zone", "Intense session"
    /// </summary>
    public string PaceLabel { get; init; } = "";
}
