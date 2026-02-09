using System.Text.Json;
using DevOpTyper.Content.Services;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Loads educational metadata overlays from Assets/Snippets/*.json.
/// Each built-in snippet JSON is deserialized as Snippet to extract all
/// educational fields (difficulty, topics, explain, scaffolds, demonstrations,
/// layers, perspectives), then keyed by content-hash ID.
/// </summary>
public sealed class BuiltinOverlayStore
{
    private readonly Dictionary<string, SnippetOverlay> _overlays = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _legacyToContent = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    /// <summary>
    /// Whether any overlays have been loaded.
    /// </summary>
    public bool HasOverlays => _overlays.Count > 0;

    /// <summary>
    /// Number of overlays loaded.
    /// </summary>
    public int OverlayCount => _overlays.Count;

    /// <summary>
    /// Load overlays from all built-in snippet files.
    /// Safe to call multiple times — only initializes once.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var snippetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Snippets");
        if (!Directory.Exists(snippetsDir)) return;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var file in Directory.GetFiles(snippetsDir, "*.json"))
        {
            try
            {
                var language = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                var json = File.ReadAllText(file);
                var snippets = JsonSerializer.Deserialize<List<Snippet>>(json, options);
                if (snippets == null) continue;

                foreach (var snippet in snippets)
                {
                    if (string.IsNullOrWhiteSpace(snippet.Code)) continue;

                    var lang = string.IsNullOrWhiteSpace(snippet.Language) ? language : snippet.Language.ToLowerInvariant();
                    var normalized = Normalizer.Normalize(snippet.Code, ensureTrailingNewline: true);
                    var contentId = ContentId.From(lang, normalized);

                    var overlay = new SnippetOverlay
                    {
                        ContentId = contentId,
                        LegacyId = string.IsNullOrWhiteSpace(snippet.Id) ? contentId : snippet.Id,
                        Difficulty = Math.Clamp(snippet.Difficulty, 1, 5),
                        Topics = snippet.Topics ?? Array.Empty<string>(),
                        Explain = snippet.Explain ?? Array.Empty<string>(),
                        Scaffolds = snippet.Scaffolds ?? Array.Empty<string>(),
                        Demonstrations = snippet.Demonstrations ?? Array.Empty<Demonstration>(),
                        Layers = snippet.Layers ?? Array.Empty<SkillLayer>(),
                        Perspectives = snippet.Perspectives ?? Array.Empty<ExplanationSet>()
                    };

                    _overlays[contentId] = overlay;
                    _legacyToContent[overlay.LegacyId] = contentId;
                }
            }
            catch
            {
                // Silently skip malformed files — same resilience as SnippetService
            }
        }
    }

    /// <summary>
    /// Get the overlay for a given content-hash ID.
    /// Returns null for user/corpus content (no overlay exists).
    /// </summary>
    public SnippetOverlay? GetOverlay(string contentId)
    {
        return _overlays.GetValueOrDefault(contentId);
    }

    /// <summary>
    /// Resolve a legacy snippet ID to its content-hash ID.
    /// Used for backward compatibility with session history, scaffold fade,
    /// community signals, and guidance lookups.
    /// </summary>
    public string? GetContentIdForLegacy(string legacyId)
    {
        return _legacyToContent.GetValueOrDefault(legacyId);
    }

    /// <summary>
    /// All loaded overlays (for debugging/validation).
    /// </summary>
    public IReadOnlyDictionary<string, SnippetOverlay> AllOverlays() => _overlays;
}
