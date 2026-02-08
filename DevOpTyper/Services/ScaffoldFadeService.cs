using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Computes scaffold opacity based on the user's completion history
/// for a specific snippet. Scaffolds fade as the user demonstrates
/// competence — they are aids, not permanent fixtures.
///
/// This service is stateless. Opacity is recomputed from session history
/// each time — no fade state is persisted. If the user clears their history,
/// scaffolds naturally reappear, which is correct behavior.
///
/// Display-only service. No frozen service may reference this.
/// Scaffold opacity never affects scoring, XP, difficulty, or selection.
/// </summary>
public static class ScaffoldFadeService
{
    /// <summary>
    /// Accuracy threshold for "successful" completion.
    /// Only sessions meeting this threshold count toward fading.
    /// </summary>
    private const double SuccessThreshold = 90.0;

    /// <summary>
    /// Computes the scaffold opacity for a snippet based on how many
    /// successful completions the user has.
    ///
    /// - 0 successful completions: 1.0 (fully visible)
    /// - 1 successful completion:  0.6 (dimmed)
    /// - 2 successful completions: 0.3 (barely visible)
    /// - 3+ successful completions: 0.0 (hidden)
    ///
    /// "Successful" means accuracy ≥ 90%.
    /// Fade is per-snippet, never global.
    /// </summary>
    public static double ComputeOpacity(string snippetId, SessionHistory history)
    {
        if (string.IsNullOrEmpty(snippetId) || history == null)
            return 1.0;

        int successCount = history.Records
            .Where(r => r.SnippetId == snippetId && r.Accuracy >= SuccessThreshold)
            .Take(3) // Only need to count up to 3
            .Count();

        return successCount switch
        {
            0 => 1.0,
            1 => 0.6,
            2 => 0.3,
            _ => 0.0
        };
    }
}
