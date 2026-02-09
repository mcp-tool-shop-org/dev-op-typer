using DevOpTyper.Content.Models;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Maps CodeItem + optional SnippetOverlay → Snippet.
/// The existing Snippet class acts as a ViewModel; ContentBridge populates it.
/// </summary>
public static class ContentBridge
{
    /// <summary>
    /// Create a Snippet from a CodeItem and its educational overlay (if any).
    /// Built-in items have full overlays. User/corpus items get heuristic difficulty
    /// and empty educational fields.
    /// </summary>
    public static Snippet ToSnippet(CodeItem item, SnippetOverlay? overlay)
    {
        return new Snippet
        {
            Id = overlay?.LegacyId ?? item.Id,
            Language = item.Language,
            Difficulty = overlay?.Difficulty ?? DifficultyEstimator.Estimate(item.Metrics),
            Title = item.Title,
            Code = item.Code,
            Topics = overlay?.Topics ?? item.Concepts ?? Array.Empty<string>(),
            Explain = overlay?.Explain ?? Array.Empty<string>(),
            Scaffolds = overlay?.Scaffolds ?? Array.Empty<string>(),
            Demonstrations = overlay?.Demonstrations ?? Array.Empty<Demonstration>(),
            Layers = overlay?.Layers ?? Array.Empty<SkillLayer>(),
            Perspectives = overlay?.Perspectives ?? Array.Empty<ExplanationSet>(),
            IsUserAuthored = item.Source != "builtin"
        };
    }
}
