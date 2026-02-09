using DevOpTyper.Content.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Derives a deterministic difficulty tier (1-7) from CodeMetrics.
/// No ML. No adaptation. Just stable, reproducible tiers based on
/// three bands: line count, symbol density, and nesting depth.
///
/// Scoring:
///   Lines:          1-5 → 0   |  6-15 → 1   | 16-30 → 2  | 31+ → 3
///   SymbolDensity: &lt;0.15 → 0  | 0.15-0.30 → 1 | 0.30-0.45 → 2 | &gt;0.45 → 3
///   MaxIndentDepth: 0-1 → 0   |  2 → 1      |  3 → 2     |  4+ → 3
///
/// Total 0-9 points → clamped to difficulty 1-7.
///
/// Authored difficulty (from SnippetOverlay) always takes precedence
/// over derived difficulty.
/// </summary>
public static class DifficultyEstimator
{
    /// <summary>
    /// Derives difficulty tier from code metrics. Returns 1-7.
    /// </summary>
    public static int Estimate(CodeMetrics metrics)
    {
        int score = LinesBand(metrics.Lines)
                  + DensityBand(metrics.SymbolDensity)
                  + DepthBand(metrics.MaxIndentDepth);

        // Map 0-9 → 1-7 (linear scale, clamped)
        return Math.Clamp(1 + score * 6 / 9, 1, 7);
    }

    private static int LinesBand(int lines)
    {
        if (lines <= 5) return 0;
        if (lines <= 15) return 1;
        if (lines <= 30) return 2;
        return 3;
    }

    private static int DensityBand(float density)
    {
        if (density < 0.15f) return 0;
        if (density < 0.30f) return 1;
        if (density < 0.45f) return 2;
        return 3;
    }

    private static int DepthBand(int maxIndentDepth)
    {
        if (maxIndentDepth <= 1) return 0;
        if (maxIndentDepth == 2) return 1;
        if (maxIndentDepth == 3) return 2;
        return 3;
    }
}
