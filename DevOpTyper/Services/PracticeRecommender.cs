using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Suggests what to practice next based on longitudinal data,
/// current trends, and weakness patterns.
///
/// All suggestions are optional. The user can always ignore them
/// and pick whatever they want. The recommender never forces.
/// </summary>
public sealed class PracticeRecommender
{
    private readonly TrendAnalyzer _trendAnalyzer = new();
    private readonly FatigueDetector _fatigueDetector = new();

    /// <summary>
    /// Produces a set of practice suggestions given the current state.
    /// Returns an empty list if there's not enough data to suggest anything.
    /// </summary>
    public List<PracticeSuggestion> Suggest(
        PersistedBlob blob,
        string currentLanguage)
    {
        var suggestions = new List<PracticeSuggestion>();
        var data = blob.Longitudinal;
        var heatmap = blob.Profile.Heatmap;

        // Check fatigue first — if high intensity, suggest lighter work
        var cadence = _fatigueDetector.Observe(data);
        if (cadence?.Signal == FatigueSignal.HighIntensity)
        {
            suggestions.Add(new PracticeSuggestion
            {
                Type = SuggestionType.TakeBreak,
                Title = "Consider a break",
                Reason = $"{cadence.SessionsLast30Min} sessions in 30 min with declining accuracy",
                Priority = SuggestionPriority.Gentle,
                Intent = PracticeIntent.Warmup,
                Action = SuggestionAction.None // No click action — just advice
            });
        }

        // Weakness-based suggestion
        var weakest = heatmap.GetWeakest(count: 3, minAttempts: 5);
        if (weakest.Count > 0)
        {
            var topWeak = weakest[0];
            string charDisplay = FormatChar(topWeak.Character);
            string weakChars = string.Join(",", weakest.Take(3).Select(w => w.Character));
            suggestions.Add(new PracticeSuggestion
            {
                Type = SuggestionType.TargetWeakness,
                Title = $"Practice {charDisplay}",
                Reason = $"{topWeak.ErrorRate:P0} error rate over {topWeak.TotalAttempts} attempts",
                Priority = SuggestionPriority.Normal,
                Intent = PracticeIntent.WeaknessTarget,
                Focus = topWeak.Group.ToString().ToLowerInvariant(),
                Action = SuggestionAction.LoadWeaknessSnippet,
                ActionPayload = weakChars
            });
        }

        // Language trend suggestion
        var lang = currentLanguage?.ToLowerInvariant() ?? "";
        if (data.TrendsByLanguage.TryGetValue(lang, out var trend))
        {
            var summary = _trendAnalyzer.Analyze(lang, trend);
            if (summary != null)
            {
                // Declining trend — suggest lighter practice, not alarm.
                // Short-term dips are normal. Only surface this when
                // the trend analyzer has confirmed a persistent decline.
                if (summary.OverallMomentum == Momentum.Negative ||
                    summary.OverallMomentum == Momentum.StrongNegative)
                {
                    suggestions.Add(new PracticeSuggestion
                    {
                        Type = SuggestionType.AddressTrend,
                        Title = $"Try some lighter {lang} snippets",
                        Reason = summary.AccuracyDirection == TrendDirection.Declining
                            ? "Recent accuracy dipped a bit"
                            : "Recent speed dipped a bit",
                        Priority = SuggestionPriority.Low, // Low, not Normal — stay calm
                        Intent = PracticeIntent.WeaknessTarget,
                        Focus = lang,
                        Action = SuggestionAction.LoadEasySnippet
                    });
                }

                // Strong positive — suggest exploration
                if (summary.OverallMomentum == Momentum.StrongPositive &&
                    summary.SessionCount >= 20)
                {
                    suggestions.Add(new PracticeSuggestion
                    {
                        Type = SuggestionType.Explore,
                        Title = "Try harder snippets",
                        Reason = $"Strong improvement trend ({summary.WpmVelocity:+0.0;-0.0} WPM/session)",
                        Priority = SuggestionPriority.Low,
                        Intent = PracticeIntent.Exploration,
                        Action = SuggestionAction.LoadHarderSnippet
                    });
                }
            }
        }

        // Neglected language suggestion
        var neglected = FindNeglectedLanguage(data, currentLanguage);
        if (neglected != null)
        {
            suggestions.Add(new PracticeSuggestion
            {
                Type = SuggestionType.Explore,
                Title = $"Revisit {neglected}",
                Reason = $"Haven't practiced {neglected} in a while",
                Priority = SuggestionPriority.Low,
                Intent = PracticeIntent.Exploration,
                Focus = neglected,
                Action = SuggestionAction.SwitchLanguage,
                ActionPayload = neglected
            });
        }

        // Warmup suggestion for fresh sessions — warm language for long gaps
        if (cadence?.Signal == FatigueSignal.Fresh && blob.History.TotalSessions > 10)
        {
            // Long gap: frame as natural return, not absence
            string reason = cadence.MinutesSinceLastSession switch
            {
                > 43200 => "Pick up where you left off", // 30+ days
                > 1440 => "Ease back in",                // 1+ day
                > 120 => "It's been a bit",              // 2+ hours
                _ => "Start easy, build momentum"
            };

            suggestions.Add(new PracticeSuggestion
            {
                Type = SuggestionType.Warmup,
                Title = "Start with a warmup",
                Reason = reason,
                Priority = SuggestionPriority.Low,
                Intent = PracticeIntent.Warmup,
                Action = SuggestionAction.LoadEasySnippet
            });
        }

        return suggestions
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Type)
            .ToList();
    }

    /// <summary>
    /// Finds a language the user has practiced before but not recently.
    /// Returns null if no neglected language exists or user only knows one.
    /// </summary>
    private static string? FindNeglectedLanguage(LongitudinalData data, string? currentLanguage)
    {
        var current = currentLanguage?.ToLowerInvariant() ?? "";
        var cutoff = DateTime.UtcNow.AddDays(-7);

        return data.TrendsByLanguage
            .Where(kvp => kvp.Key != current
                && kvp.Value.TotalSessions >= 3
                && kvp.Value.LastSessionAt < cutoff)
            .OrderBy(kvp => kvp.Value.LastSessionAt)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();
    }

    private static string FormatChar(char c)
    {
        return c switch
        {
            ' ' => "spaces",
            '\t' => "tabs",
            '\n' or '\r' => "line endings",
            _ => $"'{c}'"
        };
    }
}

/// <summary>
/// A single practice suggestion. Optional and ignorable.
/// </summary>
public sealed class PracticeSuggestion
{
    /// <summary>What kind of suggestion this is.</summary>
    public SuggestionType Type { get; init; }

    /// <summary>Short title for display (e.g., "Practice '{'").</summary>
    public string Title { get; init; } = "";

    /// <summary>Why this is being suggested.</summary>
    public string Reason { get; init; } = "";

    /// <summary>How important this suggestion is.</summary>
    public SuggestionPriority Priority { get; init; }

    /// <summary>Intent to attach if the user follows this suggestion.</summary>
    public PracticeIntent Intent { get; init; }

    /// <summary>Optional focus to attach to the practice context.</summary>
    public string? Focus { get; init; }

    /// <summary>
    /// What action to take when the user follows this suggestion.
    /// None = display-only suggestion with no click action.
    /// </summary>
    public SuggestionAction Action { get; init; } = SuggestionAction.None;

    /// <summary>
    /// Payload for the action. Interpretation depends on Action:
    /// - LoadWeaknessSnippet: comma-separated weak chars
    /// - SwitchLanguage: language name
    /// - LoadEasySnippet: (unused, uses current language)
    /// </summary>
    public string? ActionPayload { get; init; }
}

/// <summary>
/// Categories of practice suggestions.
/// </summary>
public enum SuggestionType
{
    /// <summary>Target a specific weakness.</summary>
    TargetWeakness,

    /// <summary>Address a declining trend.</summary>
    AddressTrend,

    /// <summary>Try something new or revisit an old language.</summary>
    Explore,

    /// <summary>Start with easy practice.</summary>
    Warmup,

    /// <summary>Suggests considering a break (never enforced).</summary>
    TakeBreak
}

/// <summary>
/// How prominently a suggestion should be displayed.
/// </summary>
public enum SuggestionPriority
{
    /// <summary>Show subtly — user may or may not notice.</summary>
    Low = 0,

    /// <summary>Show normally — visible but not intrusive.</summary>
    Normal = 1,

    /// <summary>Show gently — noticeable but never alarming.</summary>
    Gentle = 2
}

/// <summary>
/// What action follows a suggestion when the user clicks it.
/// </summary>
public enum SuggestionAction
{
    /// <summary>No action — display-only suggestion (e.g., "Consider a break").</summary>
    None = 0,

    /// <summary>Load a snippet targeting specific weak characters.</summary>
    LoadWeaknessSnippet = 1,

    /// <summary>Load an easy snippet for warmup practice.</summary>
    LoadEasySnippet = 2,

    /// <summary>Switch to a different language and load a snippet.</summary>
    SwitchLanguage = 3,

    /// <summary>Load a harder snippet for the current language.</summary>
    LoadHarderSnippet = 4
}
