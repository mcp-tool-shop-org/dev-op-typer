using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Detects factual patterns from session history and longitudinal data.
/// All observations are descriptive — never prescriptive, never evaluative.
/// Uses neutral language: "tends to", "averages", "compared to".
/// Never uses: "you should", "consider", "try", "improve".
///
/// AUTOMATION GUARD: This service returns List&lt;string&gt; only.
/// It never returns structured data, actions, or commands.
/// No service may consume its output for snippet selection,
/// difficulty adjustment, or any automated decision-making.
/// </summary>
public static class PatternDetector
{
    /// <summary>
    /// Produces up to 3 factual observations from the user's data.
    /// Returns an empty list if there isn't enough data to observe patterns.
    /// </summary>
    public static List<string> Detect(SessionHistory history, LongitudinalData longitudinal)
    {
        var observations = new List<string>();

        if (history.TotalSessions < 10)
            return observations; // Not enough data for patterns

        // Pattern 1: Cross-language comparison
        var langObs = DetectLanguageDifference(longitudinal);
        if (langObs != null) observations.Add(langObs);

        // Pattern 2: Time-of-day tendency
        var timeObs = DetectTimeOfDayPattern(history);
        if (timeObs != null) observations.Add(timeObs);

        // Pattern 3: Difficulty correlation
        if (observations.Count < 3)
        {
            var diffObs = DetectDifficultyCorrelation(history);
            if (diffObs != null) observations.Add(diffObs);
        }

        // Pattern 4: Practice rhythm — session burst size
        if (observations.Count < 3)
        {
            var burstObs = DetectSessionBurstPattern(longitudinal);
            if (burstObs != null) observations.Add(burstObs);
        }

        // Pattern 5: Day-of-week preference
        if (observations.Count < 3)
        {
            var dayObs = DetectDayOfWeekPattern(history);
            if (dayObs != null) observations.Add(dayObs);
        }

        return observations.Take(3).ToList();
    }

    /// <summary>
    /// Compares average WPM across languages with enough data.
    /// </summary>
    private static string? DetectLanguageDifference(LongitudinalData data)
    {
        var eligible = data.TrendsByLanguage
            .Where(kv => kv.Value.TotalSessions >= 5)
            .Select(kv => (Language: kv.Key, AvgWpm: kv.Value.AverageWpm(20) ?? 0))
            .Where(x => x.AvgWpm > 0)
            .OrderByDescending(x => x.AvgWpm)
            .ToList();

        if (eligible.Count < 2) return null;

        var fastest = eligible.First();
        var slowest = eligible.Last();
        double delta = fastest.AvgWpm - slowest.AvgWpm;

        if (delta < 5) return null; // Not a meaningful difference

        return $"{fastest.Language} averages {delta:F0} WPM more than {slowest.Language}";
    }

    /// <summary>
    /// Checks if accuracy differs meaningfully by time of day.
    /// </summary>
    private static string? DetectTimeOfDayPattern(SessionHistory history)
    {
        var recent = history.Records.Take(50).ToList();
        if (recent.Count < 15) return null;

        var morning = recent.Where(r => r.CompletedAt.ToLocalTime().Hour < 12).ToList();
        var afternoon = recent.Where(r => r.CompletedAt.ToLocalTime().Hour >= 12).ToList();

        if (morning.Count < 5 || afternoon.Count < 5) return null;

        double morningAcc = morning.Average(r => r.Accuracy);
        double afternoonAcc = afternoon.Average(r => r.Accuracy);
        double delta = Math.Abs(morningAcc - afternoonAcc);

        if (delta < 3) return null; // Not meaningful

        string higher = morningAcc > afternoonAcc ? "morning" : "afternoon";
        return $"Accuracy tends to be {delta:F1}% higher in the {higher}";
    }

    /// <summary>
    /// Checks if accuracy drops at higher difficulties.
    /// </summary>
    private static string? DetectDifficultyCorrelation(SessionHistory history)
    {
        var recent = history.Records.Take(50).ToList();
        if (recent.Count < 10) return null;

        var easy = recent.Where(r => r.Difficulty <= 2).ToList();
        var hard = recent.Where(r => r.Difficulty >= 4).ToList();

        if (easy.Count < 3 || hard.Count < 3) return null;

        double easyAcc = easy.Average(r => r.Accuracy);
        double hardAcc = hard.Average(r => r.Accuracy);
        double delta = easyAcc - hardAcc;

        if (delta < 5) return null; // Not meaningful

        return $"Accuracy averages {delta:F0}% lower on difficulty 4-5 snippets";
    }

    /// <summary>
    /// Detects how many sessions the user typically does per sitting.
    /// Groups sessions that are within 30 minutes of each other into "bursts".
    /// </summary>
    private static string? DetectSessionBurstPattern(LongitudinalData data)
    {
        if (data.SessionTimestamps.Count < 15) return null;

        var timestamps = data.SessionTimestamps
            .OrderBy(t => t)
            .ToList();

        var burstSizes = new List<int>();
        int currentBurst = 1;

        for (int i = 1; i < timestamps.Count; i++)
        {
            if ((timestamps[i] - timestamps[i - 1]).TotalMinutes <= 30)
                currentBurst++;
            else
            {
                burstSizes.Add(currentBurst);
                currentBurst = 1;
            }
        }
        burstSizes.Add(currentBurst);

        if (burstSizes.Count < 3) return null;

        double avgBurst = burstSizes.Average();
        if (avgBurst < 1.5) return null; // Almost always single sessions

        return $"Typical practice burst: {avgBurst:F0} sessions at a time";
    }

    /// <summary>
    /// Detects if the user practices more on certain days of the week.
    /// </summary>
    private static string? DetectDayOfWeekPattern(SessionHistory history)
    {
        var recent = history.Records.Take(60).ToList();
        if (recent.Count < 20) return null;

        var weekday = recent.Where(r => r.CompletedAt.ToLocalTime().DayOfWeek is
            >= DayOfWeek.Monday and <= DayOfWeek.Friday).ToList();
        var weekend = recent.Where(r => r.CompletedAt.ToLocalTime().DayOfWeek is
            DayOfWeek.Saturday or DayOfWeek.Sunday).ToList();

        if (weekday.Count < 5 || weekend.Count < 3) return null;

        // Compare sessions per day (weekdays = 5, weekends = 2)
        double weekdayRate = (double)weekday.Count / 5;
        double weekendRate = (double)weekend.Count / 2;

        double ratio = weekdayRate / Math.Max(0.1, weekendRate);
        if (ratio > 1.8)
            return "Practices more on weekdays";
        if (ratio < 0.55)
            return "Practices more on weekends";

        return null;
    }
}
