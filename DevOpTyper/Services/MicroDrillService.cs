using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Generates targeted micro-drill sets: 5 quick snippets focused on
/// the user's top weakness category. Uses existing planner/selector
/// machinery — no new scoring systems.
///
/// Rules:
///   - Uses SessionPlanner + SmartSnippetSelector for each pick
///   - Focuses on top weakness category via SelectForWeakChars
///   - Falls back to standard adaptive selection if no weaknesses found
///   - Never changes difficulty band — picks are bounded by DifficultyProfile
///   - Each drill set is a self-contained list of snippets
/// </summary>
public sealed class MicroDrillService
{
    /// <summary>
    /// Default number of snippets in a micro-drill.
    /// </summary>
    public const int DrillSize = 5;

    private readonly SmartSnippetSelector _selector;
    private readonly WeaknessTracker _weaknessTracker;

    public MicroDrillService(SmartSnippetSelector selector, WeaknessTracker weaknessTracker)
    {
        _selector = selector;
        _weaknessTracker = weaknessTracker;
    }

    /// <summary>
    /// Generates a drill set of <paramref name="count"/> snippets focused
    /// on the user's top weakness characters.
    /// </summary>
    /// <param name="language">Target language.</param>
    /// <param name="profile">User profile with heatmap data.</param>
    /// <param name="longitudinal">Longitudinal data for weakness tracking.</param>
    /// <param name="difficultyProfile">Current difficulty profile.</param>
    /// <param name="signalPolicy">Signal policy for bias control.</param>
    /// <param name="count">Number of snippets (default 5).</param>
    /// <returns>A drill result with snippets and focus description.</returns>
    public DrillResult GenerateDrill(
        string language,
        Profile profile,
        LongitudinalData longitudinal,
        DifficultyProfile? difficultyProfile,
        SignalPolicy? signalPolicy = null,
        int count = DrillSize)
    {
        var weaknessReport = _weaknessTracker.GetReport(language, profile.Heatmap, longitudinal);

        // Determine focus characters from heatmap
        var weakChars = new HashSet<char>();
        string focusDescription = "General practice";

        if (profile.Heatmap.Records.Count > 0)
        {
            var weakest = profile.Heatmap.GetWeakest(10, minAttempts: 5);
            if (weakest.Count > 0)
            {
                foreach (var cw in weakest)
                    weakChars.Add(cw.Character);

                // Describe focus by top group
                var topGroups = profile.Heatmap.GetWeakestGroups(minAttempts: 10)
                    .Take(2)
                    .Select(g => g.Group.ToString().ToLowerInvariant());
                focusDescription = $"Targeting {string.Join(" & ", topGroups)} weaknesses";
            }
        }

        // Select snippets using weakness-focused selection
        var snippets = new List<(Snippet Snippet, SessionPlan Plan)>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            Snippet snippet;
            SessionPlan plan;

            if (weakChars.Count > 0)
            {
                // Prefer weakness-focused selection
                snippet = _selector.SelectForWeakChars(language, profile, weakChars);
                plan = new SessionPlan
                {
                    Category = MixCategory.Target,
                    TargetDifficulty = difficultyProfile?.TargetDifficulty ?? snippet.Difficulty,
                    ActualDifficulty = snippet.Difficulty,
                    ComfortZone = difficultyProfile?.TargetDifficulty,
                    Reason = $"Drill: {focusDescription}"
                };
            }
            else
            {
                // Fall back to standard adaptive selection
                (snippet, plan) = SessionPlanner.PlanNext(
                    _selector, language, profile,
                    difficultyProfile, weaknessReport,
                    signalPolicy: signalPolicy);
            }

            // Avoid exact duplicates within the drill
            if (usedIds.Contains(snippet.Id) && i < count + 3)
            {
                i--;
                continue;
            }

            usedIds.Add(snippet.Id);
            snippets.Add((snippet, plan));
        }

        return new DrillResult
        {
            Snippets = snippets,
            FocusDescription = focusDescription,
            WeakChars = weakChars
        };
    }
}

/// <summary>
/// Result of a micro-drill generation.
/// </summary>
public sealed class DrillResult
{
    /// <summary>
    /// The snippets and their plans for this drill.
    /// </summary>
    public List<(Snippet Snippet, SessionPlan Plan)> Snippets { get; init; } = new();

    /// <summary>
    /// Human-readable description of what this drill focuses on.
    /// </summary>
    public string FocusDescription { get; init; } = "";

    /// <summary>
    /// The weak characters this drill targets.
    /// </summary>
    public HashSet<char> WeakChars { get; init; } = new();
}
