using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Loads and queries anonymized aggregate signals from community content.
///
/// Display-only service. No frozen service (TypingEngine, SessionState,
/// PersistenceService, SmartSnippetSelector, AdaptiveDifficultyEngine,
/// TrendAnalyzer, WeaknessTracker, TypistIdentityService) may reference
/// this service or its data.
///
/// Signals are informational hints — they never alter difficulty, scoring,
/// snippet selection, or any runtime behavior. They exist to provide
/// gentle collective context ("most people type this at ~45 WPM") without
/// creating comparison or competition.
/// </summary>
public sealed class CommunitySignalService
{
    private readonly Dictionary<string, AggregateSignal> _signals = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    /// <summary>
    /// Whether any signals were loaded.
    /// </summary>
    public bool HasSignals => _signals.Count > 0;

    /// <summary>
    /// Number of signals loaded.
    /// </summary>
    public int SignalCount => _signals.Count;

    /// <summary>
    /// Loads signals from the community content directory.
    /// Looks for signals.json in the root of CommunityContent/.
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
        var signalsPath = Path.Combine(communityDir, "signals.json");

        if (!File.Exists(signalsPath))
            return;

        LoadSignalsFile(signalsPath);
    }

    /// <summary>
    /// Gets the aggregate signal for a specific snippet.
    /// Returns null if no signal exists for that snippet.
    /// </summary>
    public AggregateSignal? GetSignal(string snippetId)
    {
        if (string.IsNullOrEmpty(snippetId))
            return null;

        return _signals.TryGetValue(snippetId, out var signal) ? signal : null;
    }

    private void LoadSignalsFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var collection = JsonSerializer.Deserialize<SignalCollection>(json, options);
            if (collection?.Signals == null)
                return;

            foreach (var signal in collection.Signals)
            {
                if (!string.IsNullOrWhiteSpace(signal.SnippetId))
                {
                    _signals[signal.SnippetId] = signal;
                }
            }
        }
        catch
        {
            // Malformed signals.json — silently ignore.
            // Signals are optional enhancement, never critical.
        }
    }
}
