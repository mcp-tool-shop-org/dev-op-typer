namespace DevOpTyper.Models;

/// <summary>
/// Declares the extension boundaries for v0.8.2.
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

    /// <summary>
    /// Community-shared content lives in a separate directory from user-authored content.
    /// At runtime, community content is indistinguishable from user content —
    /// the separation is organizational only.
    /// </summary>
    public const string CommunityContentDir = "CommunityContent";

    /// <summary>
    /// Maximum number of community snippet files the system will load.
    /// Higher than user limit because community content may accumulate
    /// from multiple bundle imports over time.
    /// </summary>
    public const int MaxCommunitySnippetFiles = 100;

    /// <summary>
    /// Maximum number of community practice configurations.
    /// </summary>
    public const int MaxCommunityConfigs = 40;

    /// <summary>
    /// Maximum number of perspectives per snippet.
    /// Prevents UI clutter from unbounded explanation sets.
    /// </summary>
    public const int MaxPerspectivesPerSnippet = 5;

    /// <summary>
    /// Maximum number of notes per perspective.
    /// Keeps individual perspectives concise and scannable.
    /// </summary>
    public const int MaxNotesPerPerspective = 10;

    /// <summary>
    /// Maximum length of a single explanation note (chars).
    /// Short notes stay descriptive. Long notes tend toward prescriptive
    /// instructions, which undermines the "context, not instruction" principle.
    /// </summary>
    public const int MaxExplanationNoteLength = 300;

    /// <summary>
    /// Maximum number of scaffold hints per snippet.
    /// Scaffolds are short cues — too many defeats their purpose.
    /// </summary>
    public const int MaxScaffoldHints = 5;

    /// <summary>
    /// Maximum length of a single scaffold hint (chars).
    /// Hints must be brief enough to scan at a glance.
    /// </summary>
    public const int MaxScaffoldHintLength = 200;

    /// <summary>
    /// Maximum number of alternative demonstrations per snippet.
    /// Keeps the demonstration panel focused and scannable.
    /// </summary>
    public const int MaxDemonstrationsPerSnippet = 3;

    /// <summary>
    /// Maximum code length for a single demonstration (chars).
    /// </summary>
    public const int MaxDemonstrationCodeLength = 3000;

    /// <summary>
    /// Maximum description length for a demonstration (chars).
    /// </summary>
    public const int MaxDemonstrationDescriptionLength = 200;

    /// <summary>
    /// Maximum number of guidance notes per snippet.
    /// Guidance notes are short collective observations — too many overwhelms.
    /// </summary>
    public const int MaxGuidanceNotesPerSnippet = 5;

    /// <summary>
    /// Maximum length of a single guidance note (chars).
    /// Notes must be brief observations, not instruction.
    /// </summary>
    public const int MaxGuidanceNoteLength = 200;

    /// <summary>
    /// Maximum number of skill layers per snippet.
    /// Layers offer depth, not breadth — 3-4 is typical.
    /// </summary>
    public const int MaxLayersPerSnippet = 4;

    /// <summary>
    /// Maximum number of content items per layer.
    /// </summary>
    public const int MaxContentPerLayer = 8;

    /// <summary>
    /// Maximum length of a single layer content item (chars).
    /// </summary>
    public const int MaxLayerContentLength = 300;

    // ────────────────────────────────────────────────────────
    //  CONTENT LIBRARY (v0.8.1)
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Filename for the content library index.
    /// Stored at %LOCALAPPDATA%/DevOpTyper/library.index.json.
    /// Contains only user and corpus CodeItems — built-ins are
    /// reconstructed from Assets/Snippets/ at every startup.
    /// </summary>
    public const string LibraryIndexFile = "library.index.json";

    /// <summary>
    /// Maximum number of user-pasted items in the library.
    /// </summary>
    public const int MaxLibraryUserItems = 500;

    /// <summary>
    /// Maximum number of corpus-imported items in the library.
    /// </summary>
    public const int MaxLibraryCorpusItems = 2000;

    /// <summary>
    /// Maximum length of pasted code (chars).
    /// </summary>
    public const int MaxPasteLength = 10000;

    /// <summary>
    /// Maximum file size for folder import (bytes). Files larger
    /// than 2 MB are skipped during folder indexing.
    /// </summary>
    public const int MaxImportFileSize = 2 * 1024 * 1024;

    // ────────────────────────────────────────────────────────
    //  FROZEN — the system guarantees these remain stable
    //
    //  Core services (TypingEngine, SessionState, PersistenceService,
    //  SmartSnippetSelector, AdaptiveDifficultyEngine, TrendAnalyzer,
    //  WeaknessTracker, TypistIdentityService) must not be affected
    //  by extensions.
    //
    //  Data formats (PersistedBlob, SessionHistory, LongitudinalData,
    //  MistakeHeatmap) must remain backward-compatible.
    //
    //  Trust guarantees:
    //  - Accuracy and WPM computation are always correct
    //  - XP formula is consistent across all content
    //  - Session records are identical for built-in and user content
    //  - No extension can prevent the user from typing
    //  - No extension can alter persisted history
    //  - No extension introduces network calls or telemetry
    //
    //  v0.7.0 additions — these must NEVER reference frozen services:
    //  - CommunitySignalService (display-only aggregate hints)
    //  - CommunityContentService (community snippet loading)
    //  - AggregateSignal / SignalCollection (signal data models)
    //  - ExplanationSet (perspective data model)
    //  - ExplanationPanel (perspective UI)
    //
    //  Specifically, no frozen service may read AggregateSignal
    //  data. Signals must never influence difficulty selection,
    //  scoring, XP calculation, or snippet ordering.
    //
    //  v0.8.0 additions — these must NEVER reference frozen services:
    //  - ScaffoldFadeService (scaffold opacity computation)
    //  - Demonstration (alternative approach model)
    //  - DemonstrationPanel (alternative approach UI)
    //  - GuidanceNote / GuidanceCollection (guidance data models)
    //  - GuidanceService (guidance loading)
    //  - SkillLayer (skill layer model)
    //  - LayersPanel (skill layer UI)
    //
    //  Scaffolds, demonstrations, guidance, and skill layers are
    //  display-only teaching features. They must never influence
    //  difficulty selection, scoring, XP, or snippet ordering.
    //
    //  These are documented in DOCS/AGENCY.md. They exist as comments
    //  (not runtime arrays) because they are design constraints,
    //  not data the app operates on.
    // ────────────────────────────────────────────────────────

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

            if (s.Difficulty < 1 || s.Difficulty > 7)
                return $"{fileName}/{s.Id}: difficulty must be 1-7 (got {s.Difficulty})";
        }

        return null; // Valid
    }
}
