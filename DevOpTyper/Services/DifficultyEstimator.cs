using DevOpTyper.Content.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Estimates snippet difficulty from CodeMetrics for content that lacks
/// a manually assigned difficulty (user-pasted, corpus-imported).
/// </summary>
public static class DifficultyEstimator
{
    /// <summary>
    /// Estimate difficulty 1–5 from code metrics.
    /// Uses line count, symbol density, and indentation depth as signals.
    /// </summary>
    public static int Estimate(CodeMetrics metrics)
    {
        int score = 0;

        score += metrics.Lines switch
        {
            < 5 => 0,
            < 15 => 1,
            < 30 => 2,
            _ => 3
        };

        score += metrics.SymbolDensity switch
        {
            < 0.15f => 0,
            < 0.25f => 1,
            < 0.35f => 2,
            _ => 3
        };

        score += metrics.MaxIndentDepth switch
        {
            < 2 => 0,
            < 4 => 1,
            _ => 2
        };

        return Math.Clamp((int)Math.Round(score / 2.5) + 1, 1, 5);
    }
}
