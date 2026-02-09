namespace DevOpTyper.Models;

/// <summary>
/// The category of a planned snippet in the session mix.
/// Target = at the user's current working level (50% of sessions).
/// Review = slightly below comfort zone, reinforcing mastery (30%).
/// Stretch = slightly above comfort zone, pushing growth (20%).
/// </summary>
public enum MixCategory
{
    Target,
    Review,
    Stretch
}

/// <summary>
/// A planned snippet selection with reasoning. The planner produces this
/// to explain why a snippet was chosen and what category it fills.
/// Display-only metadata — never affects engine scoring or rating.
/// </summary>
public sealed class SessionPlan
{
    /// <summary>
    /// Which mix category this snippet fills.
    /// </summary>
    public MixCategory Category { get; init; }

    /// <summary>
    /// The target difficulty band for this selection.
    /// </summary>
    public int TargetDifficulty { get; init; }

    /// <summary>
    /// The actual difficulty of the selected snippet.
    /// </summary>
    public int ActualDifficulty { get; init; }

    /// <summary>
    /// The user's current comfort zone difficulty (from DifficultyProfile).
    /// Null if no comfort zone is established yet.
    /// </summary>
    public int? ComfortZone { get; init; }

    /// <summary>
    /// Short human-readable reason for this selection.
    /// Examples: "Reinforcing D3 mastery", "Stretching to D5", "Practicing at D4".
    /// </summary>
    public string Reason { get; init; } = "";
}
