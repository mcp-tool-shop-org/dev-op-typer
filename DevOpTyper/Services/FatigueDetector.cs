using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Analyzes session cadence to observe fatigue patterns.
/// Pure observation — never blocks, never shames, never prescribes.
///
/// The detector answers: "What does the recent session pattern look like?"
/// It does NOT answer: "Should the user stop?"
/// </summary>
public sealed class FatigueDetector
{
    /// <summary>
    /// Analyzes recent session timestamps and returns a cadence observation.
    /// Returns null if fewer than 3 timestamps available.
    /// </summary>
    public CadenceObservation? Observe(LongitudinalData data)
    {
        if (data.SessionTimestamps.Count < 3) return null;

        var timestamps = data.SessionTimestamps
            .OrderByDescending(t => t)
            .ToList();

        var now = DateTime.UtcNow;
        var todaySessions = CountSessionsSince(timestamps, now.Date);
        var lastHourSessions = CountSessionsSince(timestamps, now.AddHours(-1));
        var last30MinSessions = CountSessionsSince(timestamps, now.AddMinutes(-30));

        // Compute average gap between recent sessions (last 10)
        var recentGaps = ComputeGaps(timestamps.Take(10).ToList());
        double avgGapMinutes = recentGaps.Count > 0
            ? recentGaps.Average(g => g.TotalMinutes)
            : 0;

        // Detect if accuracy is declining in recent sessions (needs history)
        var recentRecords = GetRecentSessionRecords(data, 5);
        bool accuracyDeclining = IsAccuracyDeclining(recentRecords);

        // Determine fatigue signal
        var signal = ClassifySignal(
            last30MinSessions, lastHourSessions, todaySessions,
            avgGapMinutes, accuracyDeclining);

        return new CadenceObservation
        {
            SessionsToday = todaySessions,
            SessionsLastHour = lastHourSessions,
            SessionsLast30Min = last30MinSessions,
            AverageGapMinutes = Math.Round(avgGapMinutes, 1),
            Signal = signal,
            AccuracyDeclining = accuracyDeclining,
            LastSessionAt = timestamps.FirstOrDefault(),
            MinutesSinceLastSession = timestamps.Count > 0
                ? Math.Round((now - timestamps[0]).TotalMinutes, 1)
                : 0
        };
    }

    /// <summary>
    /// Computes the gap since the last session in a human-readable format.
    /// Returns null if no sessions exist.
    /// </summary>
    public string? TimeSinceLastSession(LongitudinalData data)
    {
        if (data.SessionTimestamps.Count == 0) return null;

        var gap = DateTime.UtcNow - data.SessionTimestamps[0];

        return gap.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)gap.TotalMinutes}m ago",
            < 1440 => $"{(int)gap.TotalHours}h ago",
            _ => $"{(int)gap.TotalDays}d ago"
        };
    }

    private static int CountSessionsSince(List<DateTime> timestamps, DateTime since)
    {
        return timestamps.Count(t => t >= since);
    }

    private static List<TimeSpan> ComputeGaps(List<DateTime> timestamps)
    {
        var gaps = new List<TimeSpan>();
        for (int i = 0; i < timestamps.Count - 1; i++)
        {
            gaps.Add(timestamps[i] - timestamps[i + 1]);
        }
        return gaps;
    }

    /// <summary>
    /// Gets WPM/accuracy from the most recent sessions by looking at trends.
    /// Returns pairs of (wpm, accuracy) from the LanguageTrend data.
    /// </summary>
    private static List<(double wpm, double accuracy)> GetRecentSessionRecords(
        LongitudinalData data, int count)
    {
        // Use the most active language's trend data
        var mostActive = data.TrendsByLanguage.Values
            .OrderByDescending(t => t.LastSessionAt)
            .FirstOrDefault();

        if (mostActive == null) return new();

        int n = Math.Min(count, Math.Min(mostActive.RecentWpm.Count, mostActive.RecentAccuracy.Count));
        var results = new List<(double, double)>(n);
        for (int i = 0; i < n; i++)
        {
            results.Add((mostActive.RecentWpm[i], mostActive.RecentAccuracy[i]));
        }
        return results;
    }

    /// <summary>
    /// Checks if accuracy shows a declining pattern in recent sessions.
    /// True if the most recent 2 sessions are both below the average of the set.
    /// </summary>
    private static bool IsAccuracyDeclining(List<(double wpm, double accuracy)> recent)
    {
        if (recent.Count < 4) return false;

        double avgAccuracy = recent.Average(r => r.accuracy);
        // Most recent 2 both below average
        return recent[0].accuracy < avgAccuracy && recent[1].accuracy < avgAccuracy;
    }

    private static FatigueSignal ClassifySignal(
        int last30Min, int lastHour, int today,
        double avgGapMinutes, bool accuracyDeclining)
    {
        // High burst: many sessions in 30 minutes with declining accuracy
        if (last30Min >= 5 && accuracyDeclining)
            return FatigueSignal.HighIntensity;

        // Sustained high pace: many sessions per hour
        if (lastHour >= 8 && accuracyDeclining)
            return FatigueSignal.HighIntensity;

        // Moderate pace with no decline
        if (lastHour >= 5)
            return FatigueSignal.ActivePace;

        // Warm: steady sessions with reasonable gaps
        if (today >= 3 && avgGapMinutes < 10)
            return FatigueSignal.ActivePace;

        // Normal or fresh
        if (today <= 2)
            return FatigueSignal.Fresh;

        return FatigueSignal.Steady;
    }
}

/// <summary>
/// An observation about the user's session cadence.
/// This is descriptive, not prescriptive.
/// </summary>
public sealed class CadenceObservation
{
    public int SessionsToday { get; init; }
    public int SessionsLastHour { get; init; }
    public int SessionsLast30Min { get; init; }
    public double AverageGapMinutes { get; init; }
    public FatigueSignal Signal { get; init; }
    public bool AccuracyDeclining { get; init; }
    public DateTime LastSessionAt { get; init; }
    public double MinutesSinceLastSession { get; init; }
}

/// <summary>
/// Describes the current practice intensity.
/// Named to avoid judgment — these are observations, not warnings.
/// </summary>
public enum FatigueSignal
{
    /// <summary>
    /// Few sessions today. User is fresh.
    /// </summary>
    Fresh,

    /// <summary>
    /// Moderate number of sessions at a comfortable pace.
    /// </summary>
    Steady,

    /// <summary>
    /// Many sessions in a short time, but no decline in quality.
    /// </summary>
    ActivePace,

    /// <summary>
    /// Many sessions in a short time with declining accuracy.
    /// The system observes this but does not intervene.
    /// </summary>
    HighIntensity
}
