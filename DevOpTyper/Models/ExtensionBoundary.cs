namespace DevOpTyper.Models;

/// <summary>
/// Declares the extension boundaries for v0.6.0.
///
/// This is a contract: it defines what users may extend and what the system
/// guarantees will remain stable. It exists as code (not just documentation)
/// so that violations can be caught at compile time or review time.
///
/// EXTENSIBLE means users may add, configure, or replace.
/// FROZEN means the system guarantees stability across versions.
/// </summary>
public static class ExtensionBoundary
{
    // ────────────────────────────────────────────────────────
    //  EXTENSIBLE — users may shape these
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Users may add their own snippet files alongside built-in ones.
    /// User snippets follow the same JSON schema as built-in snippets.
    /// They are loaded from a separate directory to avoid collision.
    /// </summary>
    public const string UserSnippetsDir = "UserSnippets";

    /// <summary>
    /// Users may create named practice configurations that bundle
    /// tuning parameters (difficulty bias, warmup behavior, etc).
    /// </summary>
    public const string UserConfigsDir = "UserConfigs";

    /// <summary>
    /// File extension for user-authored snippet collections.
    /// Same as built-in: JSON files, one per language or topic.
    /// </summary>
    public const string SnippetFileExtension = ".json";

    /// <summary>
    /// File extension for user practice configurations.
    /// </summary>
    public const string ConfigFileExtension = ".json";

    /// <summary>
    /// Maximum number of user snippet files the system will load.
    /// Prevents accidental performance degradation from dumping
    /// thousands of files into the directory.
    /// </summary>
    public const int MaxUserSnippetFiles = 50;

    /// <summary>
    /// Maximum number of snippets per user file.
    /// </summary>
    public const int MaxSnippetsPerFile = 200;

    /// <summary>
    /// Maximum code length for a single user snippet (chars).
    /// </summary>
    public const int MaxSnippetCodeLength = 5000;

    /// <summary>
    /// Maximum number of user practice configurations.
    /// </summary>
    public const int MaxUserConfigs = 20;

    // ────────────────────────────────────────────────────────
    //  FROZEN — the system guarantees these remain stable
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Core services that must not be affected by extensions.
    /// No user content or configuration may alter:
    /// - TypingEngine (keystroke processing, diff computation)
    /// - SessionState (XP calculation, completion detection)
    /// - PersistenceService (data format, sanitization)
    /// - TypistIdentityService (identity computation)
    /// </summary>
    public static readonly string[] FrozenServices =
    [
        "TypingEngine",
        "SessionState",
        "PersistenceService",
        "TypistIdentityService"
    ];

    /// <summary>
    /// Data formats that must remain backward-compatible.
    /// User data is never invalidated by extension features.
    /// </summary>
    public static readonly string[] FrozenFormats =
    [
        "PersistedBlob (schema v3)",
        "SessionHistory records",
        "LongitudinalData trends",
        "MistakeHeatmap entries"
    ];

    /// <summary>
    /// Behaviors that extensions may not override.
    /// These are the core trust guarantees.
    /// </summary>
    public static readonly string[] FrozenBehaviors =
    [
        "Accuracy computation is always correct",
        "WPM computation is always correct",
        "XP formula is consistent across all content",
        "Session records are identical for built-in and user content",
        "No extension can prevent the user from typing",
        "No extension can alter persisted history",
        "No extension introduces network calls or telemetry"
    ];

    /// <summary>
    /// Validates that a user snippet file won't exceed boundaries.
    /// Returns null if valid, or an error message if not.
    /// </summary>
    public static string? ValidateSnippetFile(List<Snippet> snippets, string fileName)
    {
        if (snippets.Count > MaxSnippetsPerFile)
            return $"{fileName}: exceeds {MaxSnippetsPerFile} snippet limit ({snippets.Count} found)";

        foreach (var s in snippets)
        {
            if (s.Code.Length > MaxSnippetCodeLength)
                return $"{fileName}/{s.Id}: code exceeds {MaxSnippetCodeLength} char limit ({s.Code.Length})";

            if (string.IsNullOrWhiteSpace(s.Id))
                return $"{fileName}: snippet missing required 'id' field";

            if (string.IsNullOrWhiteSpace(s.Code))
                return $"{fileName}/{s.Id}: snippet missing required 'code' field";

            if (s.Difficulty < 1 || s.Difficulty > 5)
                return $"{fileName}/{s.Id}: difficulty must be 1-5 (got {s.Difficulty})";
        }

        return null; // Valid
    }
}
