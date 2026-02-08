using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Enhances weakness reporting with improvement context.
/// Instead of just showing "you're bad at '{'" it shows
/// "'{' is improving (was 40% err, now 25% err)".
///
/// Combines current heatmap state with historical snapshots
/// to produce weakness reports that acknowledge progress.
/// </summary>
public sealed class WeaknessTracker
{
    private readonly TrendAnalyzer _trendAnalyzer = new();

    /// <summary>
    /// Produces an enriched weakness report for a language.
    /// Combines current heatmap with historical snapshots.
    /// </summary>
    public WeaknessReport GetReport(
        string language, MistakeHeatmap heatmap, LongitudinalData longitudinal)
    {
        var lang = language?.ToLowerInvariant() ?? "";
        var currentWeakest = heatmap.GetWeakest(count: 10, minAttempts: 5);

        if (currentWeakest.Count == 0)
        {
            return new WeaknessReport
            {
                Language = lang,
                HasData = false
            };
        }

        // Get historical changes
        var changes = _trendAnalyzer.CompareRecentSnapshots(longitudinal, lang);
        var changeMap = changes.ToDictionary(c => c.Character, c => c);

        // Build enriched weakness items
        var items = new List<WeaknessItem>();
        foreach (var weakness in currentWeakest)
        {
            var item = new WeaknessItem
            {
                Character = weakness.Character,
                CurrentErrorRate = weakness.ErrorRate,
                TotalAttempts = weakness.TotalAttempts,
                TotalMisses = weakness.TotalMisses,
                Group = weakness.Group,
                TopConfusion = weakness.TopConfusion
            };

            if (changeMap.TryGetValue(weakness.Character, out var change))
            {
                item.PreviousErrorRate = change.OldErrorRate;
                item.Improvement = change.Improvement;
                item.Trajectory = ClassifyTrajectory(change.Improvement, change.OldErrorRate);
            }
            else
            {
                item.Trajectory = WeaknessTrajectory.New;
            }

            items.Add(item);
        }

        // Find resolved weaknesses — chars that were weak before but aren't anymore
        var resolved = new List<ResolvedWeakness>();
        foreach (var change in changes)
        {
            bool stillWeak = currentWeakest.Any(w => w.Character == change.Character);
            if (!stillWeak && change.Improvement > 0.1)
            {
                resolved.Add(new ResolvedWeakness
                {
                    Character = change.Character,
                    OldErrorRate = change.OldErrorRate,
                    CurrentErrorRate = change.NewErrorRate
                });
            }
        }

        // Compute overall weakness trajectory
        int improving = items.Count(i => i.Trajectory == WeaknessTrajectory.Improving);
        int worsening = items.Count(i => i.Trajectory == WeaknessTrajectory.Worsening);
        int steady = items.Count(i => i.Trajectory == WeaknessTrajectory.Steady);

        return new WeaknessReport
        {
            Language = lang,
            HasData = true,
            Items = items,
            ResolvedWeaknesses = resolved,
            ImprovingCount = improving,
            WorseningCount = worsening,
            SteadyCount = steady,
            Summary = ComputeSummary(improving, worsening, steady, resolved.Count)
        };
    }

    /// <summary>
    /// Gets the single most important weakness to focus on.
    /// Prefers worsening weaknesses, then new ones, then steady.
    /// </summary>
    public WeaknessItem? GetPriorityWeakness(WeaknessReport report)
    {
        if (!report.HasData || report.Items.Count == 0) return null;

        // Priority: worsening > new > steady > improving
        return report.Items
            .OrderByDescending(i => i.Trajectory switch
            {
                WeaknessTrajectory.Worsening => 4,
                WeaknessTrajectory.New => 3,
                WeaknessTrajectory.Steady => 2,
                WeaknessTrajectory.Improving => 1,
                _ => 0
            })
            .ThenByDescending(i => i.CurrentErrorRate)
            .FirstOrDefault();
    }

    private static WeaknessTrajectory ClassifyTrajectory(double improvement, double oldErrorRate)
    {
        // Need at least some meaningful change relative to the old rate
        double threshold = Math.Max(0.03, oldErrorRate * 0.1);

        if (improvement > threshold) return WeaknessTrajectory.Improving;
        if (improvement < -threshold) return WeaknessTrajectory.Worsening;
        return WeaknessTrajectory.Steady;
    }

    private static string ComputeSummary(int improving, int worsening, int steady, int resolved)
    {
        var parts = new List<string>();

        if (resolved > 0)
            parts.Add($"{resolved} resolved");
        if (improving > 0)
            parts.Add($"{improving} improving");
        if (steady > 0)
            parts.Add($"{steady} steady");
        if (worsening > 0)
            parts.Add($"{worsening} need attention");

        return parts.Count > 0 ? string.Join(", ", parts) : "No weakness data yet";
    }
}

/// <summary>
/// A complete weakness report with improvement context.
/// </summary>
public sealed class WeaknessReport
{
    public string Language { get; init; } = "";
    public bool HasData { get; init; }
    public List<WeaknessItem> Items { get; init; } = new();
    public List<ResolvedWeakness> ResolvedWeaknesses { get; init; } = new();
    public int ImprovingCount { get; init; }
    public int WorseningCount { get; init; }
    public int SteadyCount { get; init; }
    public string Summary { get; init; } = "";
}

/// <summary>
/// A single weakness with trajectory context.
/// </summary>
public sealed class WeaknessItem
{
    public char Character { get; init; }
    public double CurrentErrorRate { get; init; }
    public int TotalAttempts { get; init; }
    public int TotalMisses { get; init; }
    public SymbolGroup Group { get; init; }
    public char TopConfusion { get; init; }

    /// <summary>Previous error rate from last snapshot. Null if no history.</summary>
    public double? PreviousErrorRate { get; set; }

    /// <summary>Improvement value (positive = better). Null if no history.</summary>
    public double? Improvement { get; set; }

    /// <summary>Whether this weakness is getting better, worse, or unchanged.</summary>
    public WeaknessTrajectory Trajectory { get; set; }
}

/// <summary>
/// A weakness that was resolved between snapshots.
/// </summary>
public sealed class ResolvedWeakness
{
    public char Character { get; init; }
    public double OldErrorRate { get; init; }
    public double CurrentErrorRate { get; init; }
}

/// <summary>
/// Direction a weakness is moving.
/// </summary>
public enum WeaknessTrajectory
{
    /// <summary>Error rate is decreasing — getting better.</summary>
    Improving,

    /// <summary>Error rate is stable — neither better nor worse.</summary>
    Steady,

    /// <summary>Error rate is increasing — getting worse.</summary>
    Worsening,

    /// <summary>Newly detected — no historical comparison available.</summary>
    New
}
