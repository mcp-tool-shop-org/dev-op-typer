using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Plans each snippet selection as Target (50%), Review (30%), or Stretch (20%).
///
/// Target: at or near the user's comfort zone — the working level.
/// Review: 1 band below comfort — reinforces mastery of familiar patterns.
/// Stretch: 1 band above comfort — gentle push toward growth.
///
/// When no DifficultyProfile is available, all selections are Target (rating-based).
/// Delegates actual snippet picking to SmartSnippetSelector after setting the difficulty.
/// The returned SessionPlan is display-only and never affects engine behavior.
/// </summary>
public static class SessionPlanner
{
    private const double TargetWeight = 0.50;
    private const double ReviewWeight = 0.30;
    // Stretch = 1 - Target - Review = 0.20

    /// <summary>
    /// Plan and select a snippet with a mix category and reasoning.
    /// </summary>
    /// <param name="selector">The snippet selector to delegate to.</param>
    /// <param name="language">Target language.</param>
    /// <param name="profile">User's profile with ratings.</param>
    /// <param name="difficultyProfile">Current difficulty profile (null if not established).</param>
    /// <param name="weaknessReport">Optional weakness report for adaptive boost.</param>
    /// <param name="manualDifficultyLock">User's manual difficulty override.</param>
    /// <param name="isYoYoing">Whether yo-yo lock is active.</param>
    /// <param name="rng">Optional Random for deterministic testing.</param>
    /// <returns>The selected snippet and the session plan explaining the choice.</returns>
    public static (Snippet Snippet, SessionPlan Plan) PlanNext(
        SmartSnippetSelector selector,
        string language,
        Profile profile,
        DifficultyProfile? difficultyProfile,
        WeaknessReport? weaknessReport,
        int? manualDifficultyLock = null,
        bool isYoYoing = false,
        Random? rng = null)
    {
        rng ??= Random.Shared;

        int? comfortZone = difficultyProfile?.TargetDifficulty;

        // Manual lock overrides everything
        if (manualDifficultyLock.HasValue)
        {
            var locked = manualDifficultyLock.Value;
            var lockedProfile = CreateProfileForDifficulty(locked);
            var snippet = selector.SelectAdaptive(language, profile, lockedProfile, weaknessReport);
            return (snippet, new SessionPlan
            {
                Category = MixCategory.Target,
                TargetDifficulty = locked,
                ActualDifficulty = snippet.Difficulty,
                ComfortZone = comfortZone,
                Reason = $"Manual lock at D{locked}"
            });
        }

        // Yo-yo lock: stabilize at comfort zone
        if (isYoYoing && comfortZone.HasValue)
        {
            var stable = comfortZone.Value;
            var stableProfile = CreateProfileForDifficulty(stable);
            var snippet = selector.SelectAdaptive(language, profile, stableProfile, weaknessReport);
            return (snippet, new SessionPlan
            {
                Category = MixCategory.Target,
                TargetDifficulty = stable,
                ActualDifficulty = snippet.Difficulty,
                ComfortZone = comfortZone,
                Reason = $"Stabilizing at D{stable} (yo-yo detected)"
            });
        }

        // No comfort zone yet: everything is Target (rating-based)
        if (!comfortZone.HasValue)
        {
            var snippet = selector.SelectAdaptive(language, profile, difficultyProfile, weaknessReport);
            return (snippet, new SessionPlan
            {
                Category = MixCategory.Target,
                TargetDifficulty = snippet.Difficulty,
                ActualDifficulty = snippet.Difficulty,
                ComfortZone = null,
                Reason = "Establishing comfort zone"
            });
        }

        // Choose mix category
        var category = ChooseCategory(rng);
        int targetDifficulty = CategoryToDifficulty(category, comfortZone.Value);

        var categoryProfile = CreateProfileForDifficulty(targetDifficulty);
        var selected = selector.SelectAdaptive(language, profile, categoryProfile, weaknessReport);

        // If the actual difficulty doesn't match target, annotate the reason
        bool mismatch = selected.Difficulty != targetDifficulty;
        string reason = category switch
        {
            MixCategory.Review when mismatch => $"Reinforcing D{selected.Difficulty} (nearest to D{targetDifficulty})",
            MixCategory.Review => $"Reinforcing D{targetDifficulty} mastery",
            MixCategory.Stretch when mismatch => $"Stretching to D{selected.Difficulty} (nearest to D{targetDifficulty})",
            MixCategory.Stretch => $"Stretching to D{targetDifficulty}",
            _ when mismatch => $"Practicing at D{selected.Difficulty} (nearest to D{targetDifficulty})",
            _ => $"Practicing at D{targetDifficulty}"
        };

        return (selected, new SessionPlan
        {
            Category = category,
            TargetDifficulty = targetDifficulty,
            ActualDifficulty = selected.Difficulty,
            ComfortZone = comfortZone,
            Reason = reason
        });
    }

    /// <summary>
    /// Chooses a mix category based on the 50/30/20 distribution.
    /// </summary>
    internal static MixCategory ChooseCategory(Random rng)
    {
        var roll = rng.NextDouble();
        if (roll < TargetWeight) return MixCategory.Target;
        if (roll < TargetWeight + ReviewWeight) return MixCategory.Review;
        return MixCategory.Stretch;
    }

    /// <summary>
    /// Maps a mix category to a target difficulty band relative to comfort zone.
    /// Review: comfort - 1 (min 1). Target: comfort. Stretch: comfort + 1 (max 7).
    /// </summary>
    internal static int CategoryToDifficulty(MixCategory category, int comfortZone)
    {
        return category switch
        {
            MixCategory.Review => Math.Max(1, comfortZone - 1),
            MixCategory.Stretch => Math.Min(7, comfortZone + 1),
            _ => comfortZone
        };
    }

    /// <summary>
    /// Creates a DifficultyProfile that targets a specific difficulty band.
    /// Used when the planner overrides the natural profile with a specific target.
    /// </summary>
    private static DifficultyProfile CreateProfileForDifficulty(int difficulty)
    {
        return new DifficultyProfile
        {
            TargetDifficulty = difficulty,
            MinDifficulty = Math.Max(1, difficulty - 1),
            MaxDifficulty = Math.Min(7, difficulty + 1),
            Confidence = 1.0,
            Reason = DifficultyReason.Static
        };
    }
}
