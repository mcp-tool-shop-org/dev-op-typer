using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Analyzes longitudinal data to produce trend summaries.
/// Pure computation — no side effects, no persistence, no UI coupling.
/// Feed it data, get observations back.
/// </summary>
public sealed class TrendAnalyzer
{
    /// <summary>
    /// Analyzes a language's trend data and returns a summary.
    /// Returns null if insufficient data (fewer than 5 sessions).
    /// </summary>
    public LanguageTrendSummary? Analyze(string language, LanguageTrend trend)
    {
        if (trend.TotalSessions < 5) return null;

        var wpmTrend = ComputeDirection(trend.RecentWpm);
        var accTrend = ComputeDirection(trend.RecentAccuracy);

        return new LanguageTrendSummary
        {
            Language = language,
            SessionCount = trend.TotalSessions,
            FirstPracticed = trend.FirstSessionAt,
            LastPracticed = trend.LastSessionAt,
            RecentAvgWpm = trend.AverageWpm(10) ?? 0,
            RecentAvgAccuracy = trend.AverageAccuracy(10) ?? 0,
            OlderAvgWpm = trend.AverageWpm(50) ?? 0,
            OlderAvgAccuracy = trend.AverageAccuracy(50) ?? 0,
            WpmDirection = wpmTrend,
            AccuracyDirection = accTrend,
            WpmVelocity = ComputeVelocity(trend.RecentWpm),
            AccuracyVelocity = ComputeVelocity(trend.RecentAccuracy),
            OverallMomentum = CombineMomentum(wpmTrend, accTrend),
            PlateauLength = ComputePlateauLength(trend.RecentWpm)
        };
    }

    /// <summary>
    /// Analyzes all languages and returns summaries for those with enough data.
    /// </summary>
    public List<LanguageTrendSummary> AnalyzeAll(LongitudinalData data)
    {
        var results = new List<LanguageTrendSummary>();
        foreach (var (lang, trend) in data.TrendsByLanguage)
        {
            var summary = Analyze(lang, trend);
            if (summary != null)
            {
                results.Add(summary);
            }
        }
        return results.OrderByDescending(s => s.LastPracticed).ToList();
    }

    /// <summary>
    /// Checks whether a character's error rate has improved between two snapshots.
    /// Returns null if the character doesn't appear in both snapshots.
    /// </summary>
    public double? MeasureWeaknessImprovement(
        WeaknessSnapshot older, WeaknessSnapshot newer, char character)
    {
        var oldEntry = older.TopWeaknesses.FirstOrDefault(w => w.Character == character);
        var newEntry = newer.TopWeaknesses.FirstOrDefault(w => w.Character == character);

        if (oldEntry == null) return null; // Wasn't a weakness before
        if (newEntry == null) return 1.0; // No longer in top weaknesses — fully improved

        // Positive = improved, negative = got worse
        return oldEntry.ErrorRate - newEntry.ErrorRate;
    }

    /// <summary>
    /// Compares the two most recent weakness snapshots for a language.
    /// Returns improvements for each character that appeared in the older snapshot.
    /// </summary>
    public List<WeaknessChange> CompareRecentSnapshots(
        LongitudinalData data, string language)
    {
        var lang = language?.ToLowerInvariant() ?? "";
        var snapshots = data.WeaknessSnapshots
            .Where(s => s.Language == lang)
            .OrderByDescending(s => s.CapturedAt)
            .Take(2)
            .ToList();

        if (snapshots.Count < 2) return new();

        var newer = snapshots[0];
        var older = snapshots[1];
        var changes = new List<WeaknessChange>();

        foreach (var entry in older.TopWeaknesses)
        {
            var improvement = MeasureWeaknessImprovement(older, newer, entry.Character);
            if (improvement.HasValue)
            {
                changes.Add(new WeaknessChange
                {
                    Character = entry.Character,
                    OldErrorRate = entry.ErrorRate,
                    NewErrorRate = newer.TopWeaknesses
                        .FirstOrDefault(w => w.Character == entry.Character)?.ErrorRate ?? 0,
                    Improvement = improvement.Value
                });
            }
        }

        return changes.OrderByDescending(c => Math.Abs(c.Improvement)).ToList();
    }

    /// <summary>
    /// Determines direction from a sequence of values (newest first).
    /// Compares recent average (last 5) to older average (last 5 before that).
    /// </summary>
    private static TrendDirection ComputeDirection(List<double> values)
    {
        if (values.Count < 10) return TrendDirection.Stable;

        double recent = values.Take(5).Average();
        double older = values.Skip(5).Take(5).Average();
        double diff = recent - older;
        double threshold = older * 0.03; // 3% change threshold

        if (diff > threshold) return TrendDirection.Improving;
        if (diff < -threshold) return TrendDirection.Declining;
        return TrendDirection.Stable;
    }

    /// <summary>
    /// Computes velocity — rate of change per session (newest first).
    /// Positive = improving, negative = declining.
    /// Returns 0 if insufficient data.
    /// </summary>
    private static double ComputeVelocity(List<double> values)
    {
        if (values.Count < 4) return 0;

        // Simple linear regression over last 10 values
        int n = Math.Min(values.Count, 10);
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        for (int i = 0; i < n; i++)
        {
            // x = session index (0 = oldest, n-1 = newest)
            double x = n - 1 - i;
            double y = values[i];
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 0.001) return 0;

        double slope = (n * sumXY - sumX * sumY) / denom;
        return Math.Round(slope, 2);
    }

    /// <summary>
    /// Counts how many recent sessions fall within a narrow band of the mean.
    /// A high count indicates a plateau — which is normal and expected.
    /// </summary>
    private static int ComputePlateauLength(List<double> values)
    {
        if (values.Count < 5) return 0;

        double mean = values.Take(10).Average();
        double band = mean * 0.05; // 5% band
        int count = 0;

        foreach (var v in values.Take(20))
        {
            if (Math.Abs(v - mean) <= band)
                count++;
            else
                break; // Consecutive plateau ended
        }

        return count;
    }

    /// <summary>
    /// Combines WPM and accuracy trends into an overall momentum signal.
    /// </summary>
    private static Momentum CombineMomentum(TrendDirection wpm, TrendDirection accuracy)
    {
        if (wpm == TrendDirection.Improving && accuracy == TrendDirection.Improving)
            return Momentum.StrongPositive;

        if (wpm == TrendDirection.Improving || accuracy == TrendDirection.Improving)
            return Momentum.Positive;

        if (wpm == TrendDirection.Declining && accuracy == TrendDirection.Declining)
            return Momentum.StrongNegative;

        if (wpm == TrendDirection.Declining || accuracy == TrendDirection.Declining)
            return Momentum.Negative;

        return Momentum.Neutral;
    }
}

/// <summary>
/// Summary of a language's practice trend.
/// </summary>
public sealed class LanguageTrendSummary
{
    public string Language { get; init; } = "";
    public int SessionCount { get; init; }
    public DateTime? FirstPracticed { get; init; }
    public DateTime? LastPracticed { get; init; }

    /// <summary>Average WPM over last 10 sessions.</summary>
    public double RecentAvgWpm { get; init; }

    /// <summary>Average accuracy over last 10 sessions.</summary>
    public double RecentAvgAccuracy { get; init; }

    /// <summary>Average WPM over all stored sessions (up to 50).</summary>
    public double OlderAvgWpm { get; init; }

    /// <summary>Average accuracy over all stored sessions (up to 50).</summary>
    public double OlderAvgAccuracy { get; init; }

    public TrendDirection WpmDirection { get; init; }
    public TrendDirection AccuracyDirection { get; init; }

    /// <summary>WPM change per session (positive = faster).</summary>
    public double WpmVelocity { get; init; }

    /// <summary>Accuracy change per session (positive = more accurate).</summary>
    public double AccuracyVelocity { get; init; }

    public Momentum OverallMomentum { get; init; }

    /// <summary>
    /// Number of consecutive recent sessions within a narrow band of the mean.
    /// High values indicate a plateau — which is normal, not a problem.
    /// </summary>
    public int PlateauLength { get; init; }
}

/// <summary>
/// Direction of a metric over time.
/// </summary>
public enum TrendDirection
{
    Improving,
    Stable,
    Declining
}

/// <summary>
/// Combined momentum across WPM and accuracy.
/// </summary>
public enum Momentum
{
    StrongPositive,
    Positive,
    Neutral,
    Negative,
    StrongNegative
}

/// <summary>
/// Change in weakness for a single character between two snapshots.
/// </summary>
public sealed class WeaknessChange
{
    public char Character { get; init; }
    public double OldErrorRate { get; init; }
    public double NewErrorRate { get; init; }

    /// <summary>
    /// Positive = improved (lower error rate), negative = got worse.
    /// 1.0 = fully resolved (no longer in top weaknesses).
    /// </summary>
    public double Improvement { get; init; }
}
