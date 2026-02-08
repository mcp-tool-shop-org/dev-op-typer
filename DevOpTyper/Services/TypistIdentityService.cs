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

        return lines;
    }
}
