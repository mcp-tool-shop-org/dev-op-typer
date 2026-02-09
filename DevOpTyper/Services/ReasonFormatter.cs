using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Formats SessionPlan metadata into human-readable pick reason strings.
/// Used by the UI to show why a snippet was chosen.
/// </summary>
public static class ReasonFormatter
{
    /// <summary>
    /// Formats a full pick reason from a SessionPlan.
    /// Returns a string like "Target: D4 — matches your comfort zone".
    /// </summary>
    public static string Format(SessionPlan plan)
    {
        var categoryIcon = CategoryIcon(plan.Category);
        var categoryLabel = CategoryLabel(plan.Category);

        if (plan.ComfortZone.HasValue)
        {
            return plan.Category switch
            {
                MixCategory.Review => $"{categoryIcon} {categoryLabel}: D{plan.TargetDifficulty} — reinforcing familiar patterns",
                MixCategory.Stretch => $"{categoryIcon} {categoryLabel}: D{plan.TargetDifficulty} — pushing toward growth",
                _ => $"{categoryIcon} {categoryLabel}: D{plan.TargetDifficulty} — at your comfort zone"
            };
        }

        return $"{categoryIcon} {categoryLabel}: {plan.Reason}";
    }

    /// <summary>
    /// Formats a short category label.
    /// </summary>
    public static string CategoryLabel(MixCategory category)
    {
        return category switch
        {
            MixCategory.Target => "Target",
            MixCategory.Review => "Review",
            MixCategory.Stretch => "Stretch",
            _ => "Practice"
        };
    }

    /// <summary>
    /// Returns a small icon for the category.
    /// </summary>
    public static string CategoryIcon(MixCategory category)
    {
        return category switch
        {
            MixCategory.Target => "\U0001F3AF",  // 🎯
            MixCategory.Review => "\U0001F504",   // 🔄
            MixCategory.Stretch => "\U0001F4AA",  // 💪
            _ => "\u2022"                          // •
        };
    }
}
