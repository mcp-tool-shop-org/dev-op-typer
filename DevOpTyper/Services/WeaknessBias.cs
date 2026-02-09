using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Provides bounded selection bias based on weakness categories.
/// When enabled via SignalPolicy, prefers snippets whose code exercises
/// the user's weakest symbol groups — without ever changing the difficulty band.
///
/// Hard constraints:
///   - Never changes the difficulty band
///   - Never changes language
///   - Bonus is capped at +MaxBias to prevent hammering
///   - Diversity guard: at least 2 categories must be weak for bias to activate
/// </summary>
public static class WeaknessBias
{
    /// <summary>
    /// Maximum bonus any single snippet can receive from category bias.
    /// Keeps bias bounded so it influences ordering, not dominates it.
    /// </summary>
    private const double MaxBias = 15.0;

    /// <summary>
    /// Minimum number of weak groups required to activate bias.
    /// Prevents hammering on a single narrow weakness.
    /// </summary>
    private const int MinWeakGroupsForActivation = 2;

    /// <summary>
    /// Minimum group error rate to be considered "weak" (15%).
    /// </summary>
    private const double WeakGroupThreshold = 0.10;

    /// <summary>
    /// Minimum attempts per group before it qualifies as a data-backed weakness.
    /// </summary>
    private const int MinGroupAttempts = 10;

    /// <summary>
    /// Computes a bounded category-level bias score for a snippet.
    /// Returns 0.0 when bias should not apply (policy off, insufficient data, etc.).
    /// </summary>
    /// <param name="snippet">The candidate snippet.</param>
    /// <param name="heatmap">User's mistake heatmap with per-char tracking.</param>
    /// <param name="policy">Signal policy controlling whether bias is active.</param>
    /// <returns>A bonus score in [0, MaxBias].</returns>
    public static double ComputeCategoryBias(Snippet snippet, MistakeHeatmap heatmap, SignalPolicy? policy)
    {
        // Gate: only active when Guided Mode enables selection bias
        if (policy?.EffectiveSelectionBias != true)
            return 0.0;

        // Get weak symbol groups from heatmap
        var weakGroups = heatmap.GetWeakestGroups(minAttempts: MinGroupAttempts)
            .Where(g => g.ErrorRate >= WeakGroupThreshold)
            .ToList();

        // Diversity guard: need multiple weak areas to avoid hammering
        if (weakGroups.Count < MinWeakGroupsForActivation)
            return 0.0;

        // Build a lookup of weak groups → error rate for scoring
        var weakGroupSet = new Dictionary<SymbolGroup, double>();
        foreach (var g in weakGroups)
            weakGroupSet[g.Group] = g.ErrorRate;

        // Scan snippet code for matching weak categories
        var foundGroups = new HashSet<SymbolGroup>();
        double totalBias = 0.0;

        foreach (char c in snippet.Code)
        {
            var group = MistakeHeatmap.GetSymbolGroup(c);
            if (weakGroupSet.TryGetValue(group, out double errorRate) && foundGroups.Add(group))
            {
                // Higher error rate = more bonus, proportional scoring
                totalBias += errorRate * 10.0;
            }
        }

        return Math.Min(totalBias, MaxBias);
    }
}
