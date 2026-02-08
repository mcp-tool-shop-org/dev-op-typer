using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Adjusts difficulty based on longitudinal performance, not just current rating.
/// Remembers trends — if a user is improving, nudge harder; if plateauing, hold steady;
/// if declining, ease back. This replaces the static rating-to-difficulty mapping.
///
/// The engine produces a DifficultyProfile — a richer signal than a single int.
/// </summary>
public sealed class AdaptiveDifficultyEngine
{
    private readonly TrendAnalyzer _trendAnalyzer = new();

    /// <summary>
    /// Computes an adaptive difficulty profile for a language.
    /// Falls back to static rating-based difficulty if insufficient longitudinal data.
    /// </summary>
    public DifficultyProfile ComputeDifficulty(
        string language, Profile profile, LongitudinalData longitudinal)
    {
        int rating = profile.GetRating(language);
        int baseDifficulty = RatingToDifficulty(rating);

        var lang = language?.ToLowerInvariant() ?? "";
        if (!longitudinal.TrendsByLanguage.TryGetValue(lang, out var trend))
        {
            // No trend data — use static difficulty
            return new DifficultyProfile
            {
                TargetDifficulty = baseDifficulty,
                MinDifficulty = Math.Max(1, baseDifficulty - 1),
                MaxDifficulty = Math.Min(5, baseDifficulty + 1),
                Reason = DifficultyReason.Static,
                Confidence = 0.3
            };
        }

        var summary = _trendAnalyzer.Analyze(lang, trend);
        if (summary == null)
        {
            // Not enough sessions for trend analysis
            return new DifficultyProfile
            {
                TargetDifficulty = baseDifficulty,
                MinDifficulty = Math.Max(1, baseDifficulty - 1),
                MaxDifficulty = Math.Min(5, baseDifficulty + 1),
                Reason = DifficultyReason.Static,
                Confidence = 0.5
            };
        }

        // Adjust based on momentum
        int adjustment = summary.OverallMomentum switch
        {
            Momentum.StrongPositive => 1,    // Push harder
            Momentum.Positive => 0,           // Stay course, slightly favor harder
            Momentum.Neutral => 0,            // Hold steady
            Momentum.Negative => 0,           // Hold steady, slightly favor easier
            Momentum.StrongNegative => -1,    // Ease back
            _ => 0
        };

        int adjusted = Math.Clamp(baseDifficulty + adjustment, 1, 5);

        // Compute range — wider range when uncertain, tighter when confident
        int rangeWidth = summary.SessionCount switch
        {
            >= 30 => 0,   // Very confident — tight range
            >= 15 => 1,   // Confident — narrow range
            _ => 1        // Still learning — allow some variance
        };

        // Factor in recent accuracy to determine if we should restrict range
        double recentAccuracy = summary.RecentAvgAccuracy;
        if (recentAccuracy < 80)
        {
            // Struggling — keep difficulty low
            adjusted = Math.Min(adjusted, baseDifficulty);
            rangeWidth = Math.Max(rangeWidth, 1);
        }
        else if (recentAccuracy > 95 && summary.RecentAvgWpm > 50)
        {
            // Cruising — make sure we're not stuck too easy
            adjusted = Math.Max(adjusted, baseDifficulty);
        }

        var reason = adjustment switch
        {
            > 0 => DifficultyReason.TrendUp,
            < 0 => DifficultyReason.TrendDown,
            _ when summary.OverallMomentum == Momentum.Positive => DifficultyReason.TrendUp,
            _ when summary.OverallMomentum == Momentum.Negative => DifficultyReason.TrendDown,
            _ => DifficultyReason.Plateau
        };

        double confidence = Math.Min(1.0, summary.SessionCount / 30.0);

        return new DifficultyProfile
        {
            TargetDifficulty = adjusted,
            MinDifficulty = Math.Max(1, adjusted - rangeWidth),
            MaxDifficulty = Math.Min(5, adjusted + rangeWidth),
            Reason = reason,
            Confidence = Math.Round(confidence, 2),
            WpmVelocity = summary.WpmVelocity,
            AccuracyVelocity = summary.AccuracyVelocity
        };
    }

    /// <summary>
    /// Static rating-to-difficulty mapping (same as SmartSnippetSelector).
    /// </summary>
    private static int RatingToDifficulty(int rating)
    {
        return rating switch
        {
            < 1000 => 1,
            < 1100 => 2,
            < 1300 => 3,
            < 1500 => 4,
            _ => 5
        };
    }
}

/// <summary>
/// A richer difficulty signal than a single integer.
/// Describes what difficulty to target, the acceptable range, and why.
/// </summary>
public sealed class DifficultyProfile
{
    /// <summary>Ideal difficulty level (1-5).</summary>
    public int TargetDifficulty { get; init; }

    /// <summary>Lowest acceptable difficulty.</summary>
    public int MinDifficulty { get; init; }

    /// <summary>Highest acceptable difficulty.</summary>
    public int MaxDifficulty { get; init; }

    /// <summary>Why this difficulty was chosen.</summary>
    public DifficultyReason Reason { get; init; }

    /// <summary>
    /// How confident the system is in this difficulty (0-1).
    /// Low = few sessions, high = many sessions with clear trend.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>WPM change per session (for display).</summary>
    public double WpmVelocity { get; init; }

    /// <summary>Accuracy change per session (for display).</summary>
    public double AccuracyVelocity { get; init; }
}

/// <summary>
/// Why a particular difficulty was selected.
/// </summary>
public enum DifficultyReason
{
    /// <summary>Based on static rating (not enough data for trends).</summary>
    Static,

    /// <summary>User is improving — nudged harder.</summary>
    TrendUp,

    /// <summary>User has plateaued — holding steady.</summary>
    Plateau,

    /// <summary>User is declining — eased back.</summary>
    TrendDown
}
