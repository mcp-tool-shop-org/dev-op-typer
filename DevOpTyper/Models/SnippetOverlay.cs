namespace DevOpTyper.Models;

/// <summary>
/// Educational metadata overlay keyed by content-hash ID.
/// Built-in snippets carry full overlays extracted from Assets/Snippets/*.json.
/// User-pasted and corpus-imported items have no overlay — they get heuristic
/// difficulty and empty educational fields.
/// </summary>
public sealed class SnippetOverlay
{
    /// <summary>
    /// Content-derived ID (SHA-256 hash from meta-content-system).
    /// </summary>
    public string ContentId { get; init; } = "";

    /// <summary>
    /// Original human-authored snippet ID (e.g., "py-guard-clause").
    /// Used to preserve session history, scaffold fade, community signals,
    /// and guidance lookups that were keyed by the legacy ID.
    /// </summary>
    public string LegacyId { get; init; } = "";

    /// <summary>
    /// Manually assigned difficulty (1–5).
    /// </summary>
    public int Difficulty { get; init; } = 1;

    public string[] Topics { get; init; } = Array.Empty<string>();
    public string[] Explain { get; init; } = Array.Empty<string>();
    public string[] Scaffolds { get; init; } = Array.Empty<string>();
    public Demonstration[] Demonstrations { get; init; } = Array.Empty<Demonstration>();
    public SkillLayer[] Layers { get; init; } = Array.Empty<SkillLayer>();
    public ExplanationSet[] Perspectives { get; init; } = Array.Empty<ExplanationSet>();
}
