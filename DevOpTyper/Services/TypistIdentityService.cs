using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Computes a longitudinal "identity" from the user's accumulated data.
/// Identity here means factual descriptors of typing habits over time —
/// not labels, scores, or evaluations.
///
/// AUTOMATION GUARD: This service returns TypistIdentity (display-only data).
/// It never triggers actions, selections, or suggestions.
/// Its output is rendered in StatsPanel and nowhere else.
/// </summary>
public static class TypistIdentityService
{
    /// <summary>
    /// Minimum total sessions before identity observations appear.
    /// </summary>
    private const int MinSessions = 15;

    /// <summary>
    /// Builds a longitudinal identity summary from accumulated data.
    /// Returns null if not enough data exists.
    /// </summary>
    public static TypistIdentity? Build(SessionHistory history, LongitudinalData longitudinal)
    {
        if (history.TotalSessions < MinSessions)
            return null;

        var identity = new TypistIdentity();

        // Primary language — the one with the most sessions
        var primaryLang = longitudinal.TrendsByLanguage
            .OrderByDescending(kv => kv.Value.TotalSessions)
            .FirstOrDefault();

        if (primaryLang.Value != null && primaryLang.Value.TotalSessions >= 5)
        {
            identity.PrimaryLanguage = primaryLang.Key;
            identity.PrimaryLanguageSessions = primaryLang.Value.TotalSessions;
        }

        // Languages practiced — count of languages with 3+ sessions
        identity.LanguageCount = longitudinal.TrendsByLanguage
            .Count(kv => kv.Value.TotalSessions >= 3);

        // WPM range across all languages (recent data)
        var allRecentWpm = longitudinal.TrendsByLanguage.Values
            .SelectMany(t => t.RecentWpm.Take(10))
            .ToList();

        if (allRecentWpm.Count >= 5)
        {
            // Use 10th and 90th percentile to avoid outliers
            allRecentWpm.Sort();
            int p10 = Math.Max(0, (int)(allRecentWpm.Count * 0.1));
            int p90 = Math.Min(allRecentWpm.Count - 1, (int)(allRecentWpm.Count * 0.9));
            identity.TypicalWpmLow = allRecentWpm[p10];
            identity.TypicalWpmHigh = allRecentWpm[p90];
        }

        // Typical accuracy
        var allRecentAcc = longitudinal.TrendsByLanguage.Values
            .SelectMany(t => t.RecentAccuracy.Take(10))
            .ToList();

        if (allRecentAcc.Count >= 5)
        {
            identity.TypicalAccuracy = allRecentAcc.Average();
        }

        // Practice span — how long the user has been practicing
        var earliest = longitudinal.TrendsByLanguage.Values
            .Where(t => t.FirstSessionAt.HasValue)
            .Select(t => t.FirstSessionAt!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min();

        var latest = longitudinal.TrendsByLanguage.Values
            .Where(t => t.LastSessionAt.HasValue)
            .Select(t => t.LastSessionAt!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        var span = latest - earliest;
        if (span.TotalDays >= 1)
        {
            identity.PracticeSpanDays = (int)span.TotalDays;
        }

        // Sessions per active day (cadence) — how often they practice when they do
        if (longitudinal.SessionTimestamps.Count >= 5)
        {
            var activeDays = longitudinal.SessionTimestamps
                .Select(t => t.Date)
                .Distinct()
                .Count();

            if (activeDays > 0)
            {
                identity.SessionsPerActiveDay =
                    (double)longitudinal.SessionTimestamps.Count / activeDays;
            }
        }

        // Total sessions
        identity.TotalSessions = history.TotalSessions;

        // WPM steadiness — coefficient of variation of recent WPM
        // Low CV = consistent; high CV = variable. No judgment on either.
        if (allRecentWpm.Count >= 10)
        {
            double mean = allRecentWpm.Average();
            if (mean > 0)
            {
                double variance = allRecentWpm.Sum(v => (v - mean) * (v - mean)) / allRecentWpm.Count;
                double stddev = Math.Sqrt(variance);
                double cv = stddev / mean;
                identity.WpmVariability = cv;
            }
        }

        // Accuracy steadiness — standard deviation of recent accuracy
        if (allRecentAcc.Count >= 10)
        {
            double mean = allRecentAcc.Average();
            double variance = allRecentAcc.Sum(v => (v - mean) * (v - mean)) / allRecentAcc.Count;
            identity.AccuracyStdDev = Math.Sqrt(variance);
        }

        // "Then vs Now" — compare early and recent sessions in primary language
        // Only shown when there's enough history for a meaningful comparison.
        if (!string.IsNullOrEmpty(identity.PrimaryLanguage) &&
            longitudinal.TrendsByLanguage.TryGetValue(identity.PrimaryLanguage, out var primaryTrend) &&
            primaryTrend.RecentWpm.Count >= 20)
        {
            // "Then" = the oldest 10 data points in the rolling buffer
            // "Now"  = the newest 10
            var nowWpm = primaryTrend.RecentWpm.Take(10).Average();
            var thenWpm = primaryTrend.RecentWpm.Skip(primaryTrend.RecentWpm.Count - 10).Take(10).Average();
            var nowAcc = primaryTrend.RecentAccuracy.Take(10).Average();
            var thenAcc = primaryTrend.RecentAccuracy.Skip(primaryTrend.RecentAccuracy.Count - 10).Take(10).Average();

            identity.ThenWpm = thenWpm;
            identity.NowWpm = nowWpm;
            identity.ThenAccuracy = thenAcc;
            identity.NowAccuracy = nowAcc;
        }

        // Active days in the last 14 calendar days
        if (longitudinal.SessionTimestamps.Count > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-14);
            identity.ActiveDaysLast14 = longitudinal.SessionTimestamps
                .Where(t => t >= cutoff)
                .Select(t => t.Date)
                .Distinct()
                .Count();
        }

        return identity;
    }
}

/// <summary>
/// Factual descriptors of a user's typing identity over time.
/// All fields are nullable — shown only when enough data exists.
/// These are observations, not evaluations.
/// </summary>
public sealed class TypistIdentity
{
    /// <summary>
    /// The language with the most sessions. Null if no clear primary.
    /// </summary>
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// How many sessions in the primary language.
    /// </summary>
    public int PrimaryLanguageSessions { get; set; }

    /// <summary>
    /// Number of languages with 3+ sessions.
    /// </summary>
    public int LanguageCount { get; set; }

    /// <summary>
    /// 10th percentile of recent WPM across all languages.
    /// </summary>
    public double? TypicalWpmLow { get; set; }

    /// <summary>
    /// 90th percentile of recent WPM across all languages.
    /// </summary>
    public double? TypicalWpmHigh { get; set; }

    /// <summary>
    /// Average recent accuracy across all languages.
    /// </summary>
    public double? TypicalAccuracy { get; set; }

    /// <summary>
    /// How many days between first and last session.
    /// </summary>
    public int? PracticeSpanDays { get; set; }

    /// <summary>
    /// Average sessions per day on days the user actually practices.
    /// </summary>
    public double? SessionsPerActiveDay { get; set; }

    /// <summary>
    /// Total lifetime sessions.
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Average WPM from the user's earliest sessions in their primary language.
    /// Used for "then vs now" factual comparison. Null if not enough data.
    /// </summary>
    public double? ThenWpm { get; set; }

    /// <summary>
    /// Average WPM from the user's most recent sessions in their primary language.
    /// </summary>
    public double? NowWpm { get; set; }

    /// <summary>
    /// Average accuracy from the user's earliest sessions in their primary language.
    /// </summary>
    public double? ThenAccuracy { get; set; }

    /// <summary>
    /// Average accuracy from the user's most recent sessions in their primary language.
    /// </summary>
    public double? NowAccuracy { get; set; }

    /// <summary>
    /// Coefficient of variation of recent WPM (stddev / mean).
    /// Lower = more consistent, higher = more variable.
    /// Not labeled "good" or "bad" — just factual.
    /// </summary>
    public double? WpmVariability { get; set; }

    /// <summary>
    /// Standard deviation of recent accuracy values.
    /// Lower = steadier accuracy across sessions.
    /// </summary>
    public double? AccuracyStdDev { get; set; }

    /// <summary>
    /// Number of distinct days with sessions in the last 14 calendar days.
    /// Not a streak — just a count. No penalty for gaps.
    /// </summary>
    public int? ActiveDaysLast14 { get; set; }

    /// <summary>
    /// Produces display-ready summary lines. Returns only lines with data.
    /// All language neutral, factual, no judgment.
    /// </summary>
    public List<string> ToDisplayLines()
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(PrimaryLanguage))
        {
            string langDisplay = PrimaryLanguage.Length > 0
                ? char.ToUpper(PrimaryLanguage[0]) + PrimaryLanguage[1..]
                : PrimaryLanguage;

            if (LanguageCount > 1)
                lines.Add($"Primary: {langDisplay} ({PrimaryLanguageSessions} sessions) · {LanguageCount} languages total");
            else
                lines.Add($"Primary: {langDisplay} ({PrimaryLanguageSessions} sessions)");
        }

        if (TypicalWpmLow.HasValue && TypicalWpmHigh.HasValue)
        {
            lines.Add($"Typical range: {TypicalWpmLow.Value:F0}–{TypicalWpmHigh.Value:F0} WPM");
        }

        if (TypicalAccuracy.HasValue)
        {
            lines.Add($"Typical accuracy: {TypicalAccuracy.Value:F0}%");
        }

        // Then vs Now — factual comparison, no judgment
        if (ThenWpm.HasValue && NowWpm.HasValue)
        {
            double wpmDelta = NowWpm.Value - ThenWpm.Value;
            string wpmSign = wpmDelta >= 0 ? "+" : "";
            string line = $"Then \u2192 Now: {ThenWpm.Value:F0} \u2192 {NowWpm.Value:F0} WPM ({wpmSign}{wpmDelta:F0})";

            if (ThenAccuracy.HasValue && NowAccuracy.HasValue)
            {
                double accDelta = NowAccuracy.Value - ThenAccuracy.Value;
                string accSign = accDelta >= 0 ? "+" : "";
                line += $" \u00b7 {ThenAccuracy.Value:F0}% \u2192 {NowAccuracy.Value:F0}% ({accSign}{accDelta:F1}%)";
            }

            lines.Add(line);
        }

        if (PracticeSpanDays.HasValue && PracticeSpanDays.Value > 0)
        {
            string spanLabel = PracticeSpanDays.Value switch
            {
                < 7 => $"{PracticeSpanDays.Value} days",
                < 30 => $"{PracticeSpanDays.Value / 7} weeks",
                _ => $"{PracticeSpanDays.Value / 30} months"
            };
            lines.Add($"{TotalSessions} sessions over {spanLabel}");
        }

        if (SessionsPerActiveDay.HasValue)
        {
            lines.Add($"~{SessionsPerActiveDay.Value:F1} sessions per active day");
        }

        // Consistency metrics — factual, never gamified
        if (WpmVariability.HasValue)
        {
            // Describe variability in neutral terms
            string label = WpmVariability.Value switch
            {
                < 0.10 => "very steady",
                < 0.20 => "steady",
                < 0.35 => "moderate variation",
                _ => "wide variation"
            };
            lines.Add($"WPM consistency: {label} (CV {WpmVariability.Value:F2})");
        }

        if (AccuracyStdDev.HasValue)
        {
            string label = AccuracyStdDev.Value switch
            {
                < 2.0 => "very steady",
                < 5.0 => "steady",
                < 10.0 => "moderate variation",
                _ => "wide variation"
            };
            lines.Add($"Accuracy consistency: {label} (\u00b1{AccuracyStdDev.Value:F1}%)");
        }

        if (ActiveDaysLast14.HasValue)
        {
            lines.Add($"{ActiveDaysLast14.Value} active days in the last 2 weeks");
        }

        return lines;
    }
}
