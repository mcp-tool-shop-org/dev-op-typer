namespace DevOpTyper.Models;

/// <summary>
/// Contextual guidance for a specific snippet, emerging from collective experience.
/// Guidance notes are short observations like "This pattern trips up most people
/// at the semicolons" — they use collective language, never directive.
///
/// Guidance is always dismissible. Dismissals are session-scoped and never tracked.
/// </summary>
public sealed class GuidanceNote
{
    /// <summary>
    /// The snippet this guidance applies to.
    /// </summary>
    public string SnippetId { get; set; } = "";

    /// <summary>
    /// Contextual observations from collective experience.
    /// Uses collective language ("often", "typically"), never directive ("you should").
    /// </summary>
    public string[] Notes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// A collection of guidance notes, typically loaded from a guidance.json file.
/// Mirrors the SignalCollection pattern from v0.7.0.
/// </summary>
public sealed class GuidanceCollection
{
    /// <summary>
    /// When this guidance data was generated.
    /// </summary>
    public string GeneratedAt { get; set; } = "";

    /// <summary>
    /// How many contributors' experience informed this guidance.
    /// Display-only context — never used for ranking or authority.
    /// </summary>
    public int ContributorCount { get; set; }

    /// <summary>
    /// The guidance notes in this collection.
    /// </summary>
    public GuidanceNote[] Guidance { get; set; } = Array.Empty<GuidanceNote>();
}
