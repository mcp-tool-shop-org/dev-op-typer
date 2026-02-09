namespace DevOpTyper.Models;

/// <summary>
/// Anonymized aggregate signal for a single snippet.
///
/// Signals reflect collective patterns, not individual performance.
/// They are purely informational — display-only hints that never alter
/// difficulty, scoring, snippet selection, or any frozen service behavior.
///
/// All fields are optional. A signal with no data is valid and means
/// "no collective observations available for this snippet."
/// </summary>
public sealed class AggregateSignal
{
    /// <summary>
    /// The snippet ID this signal applies to.
    /// </summary>
    public string SnippetId { get; set; } = "";

    /// <summary>
    /// Typical WPM observed across contributors.
    /// Null means no WPM data available.
    /// </summary>
    public double? TypicalWpm { get; set; }

    /// <summary>
    /// Typical accuracy percentage observed across contributors.
    /// Null means no accuracy data available.
    /// </summary>
    public double? TypicalAccuracy { get; set; }

    /// <summary>
    /// Common difficulty areas reported by contributors.
    /// Examples: "nested braces", "long variable names", "indentation".
    /// </summary>
    public string[] CommonDifficulties { get; set; } = Array.Empty<string>();

    /// <summary>
    /// A free-text hint from the community.
    /// Example: "Most people find the second block tricky."
    /// Collective language only — never comparative or ranking.
    /// </summary>
    public string? Hint { get; set; }
}

/// <summary>
/// A collection of aggregate signals, typically loaded from signals.json.
///
/// The collection carries metadata about when and how many contributors
/// produced the data — but no individual contributor identities.
/// </summary>
public sealed class SignalCollection
{
    /// <summary>
    /// When the signals were generated (ISO 8601).
    /// </summary>
    public string GeneratedAt { get; set; } = "";

    /// <summary>
    /// How many contributors' data was aggregated.
    /// Zero means unknown or not tracked.
    /// </summary>
    public int ContributorCount { get; set; }

    /// <summary>
    /// The aggregate signals, one per snippet.
    /// </summary>
    public AggregateSignal[] Signals { get; set; } = Array.Empty<AggregateSignal>();
}
