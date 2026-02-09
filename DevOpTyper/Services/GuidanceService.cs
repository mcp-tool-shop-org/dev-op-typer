using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Loads and queries contextual guidance notes from community content.
///
/// Display-only service. No frozen service (TypingEngine, SessionState,
/// PersistenceService, SmartSnippetSelector, AdaptiveDifficultyEngine,
/// TrendAnalyzer, WeaknessTracker, TypistIdentityService) may reference
/// this service or its data.
///
/// Guidance notes are short observations from collective experience —
/// "This pattern trips up most people at the semicolons" — using
/// collective language, never directive. They exist to provide gentle
/// context without instruction.
/// </summary>
public sealed class GuidanceService
{
    private readonly Dictionary<string, GuidanceNote> _guidance = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    /// <summary>
    /// Whether any guidance notes were loaded.
    /// </summary>
    public bool HasGuidance => _guidance.Count > 0;

    /// <summary>
    /// Number of guidance notes loaded.
    /// </summary>
    public int GuidanceCount => _guidance.Count;

    /// <summary>
    /// Loads guidance from the community content directory.
    /// Looks for guidance.json in the root of CommunityContent/.
    /// Safe to call multiple times — only initializes once.
    /// No-op if the file doesn't exist.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var communityDir = Path.Combine(appDataDir, ExtensionBoundary.CommunityContentDir);
        var guidancePath = Path.Combine(communityDir, "guidance.json");

        if (!File.Exists(guidancePath))
            return;

        LoadGuidanceFile(guidancePath);
    }

    /// <summary>
    /// Gets the guidance note for a specific snippet.
    /// Returns null if no guidance exists for that snippet.
    /// </summary>
    public GuidanceNote? GetGuidance(string snippetId)
    {
        if (string.IsNullOrEmpty(snippetId))
            return null;

        return _guidance.TryGetValue(snippetId, out var note) ? note : null;
    }

    private void LoadGuidanceFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var collection = JsonSerializer.Deserialize<GuidanceCollection>(json, options);
            if (collection?.Guidance == null)
                return;

            foreach (var note in collection.Guidance)
            {
                if (!string.IsNullOrWhiteSpace(note.SnippetId) && note.Notes != null)
                {
                    _guidance[note.SnippetId] = note;
                }
            }
        }
        catch
        {
            // Malformed guidance.json — silently ignore.
            // Guidance is optional enhancement, never critical.
        }
    }
}
