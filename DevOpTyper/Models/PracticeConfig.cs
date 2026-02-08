using System.Text.Json.Serialization;

namespace DevOpTyper.Models;

/// <summary>
/// A user-authored practice configuration.
///
/// Configs describe what the user wants from a session in plain terms:
/// "I want easier snippets," "I want a warmup first," "I want stricter
/// whitespace." They never describe engine internals.
///
/// Schema is intentionally simple — a flat JSON object with all fields
/// optional. Missing fields inherit from app defaults. Invalid values
/// are ignored, not rejected.
///
/// Example JSON:
/// {
///   "name": "Morning Warmup",
///   "description": "Start easy, loosen whitespace rules",
///   "difficultyBias": "easier",
///   "warmup": true,
///   "preferShorterSnippets": true,
///   "whitespace": "lenient"
/// }
/// </summary>
public sealed class PracticeConfig
{
    /// <summary>
    /// Config file name (derived from filename, not JSON content).
    /// Used as the display name in the UI dropdown.
    /// </summary>
    [JsonIgnore]
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional description shown as a tooltip or subtitle.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this config was authored by the user (vs built-in defaults).
    /// </summary>
    [JsonIgnore]
    public bool IsUserAuthored { get; set; }

    /// <summary>
    /// Path to the source file. Null for built-in configs.
    /// </summary>
    [JsonIgnore]
    public string? SourcePath { get; set; }

    // ────────────────────────────────────────────────────────
    //  Session shape — what the user wants from their session
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Difficulty bias: "easier", "harder", "match", or null (use adaptive).
    /// "easier" shifts target difficulty down by 1.
    /// "harder" shifts target difficulty up by 1.
    /// "match" locks to the user's current adaptive level.
    /// null means no override — use the normal adaptive engine.
    /// </summary>
    public string? DifficultyBias { get; set; }

    /// <summary>
    /// Whether to start with an easy warmup snippet before adaptive selection.
    /// When true, the first snippet of a session uses difficulty 1-2 regardless
    /// of adaptive level. Subsequent snippets use normal selection.
    /// </summary>
    public bool? Warmup { get; set; }

    /// <summary>
    /// Prefer shorter snippets (under 150 chars) when true.
    /// Prefer longer snippets (over 300 chars) when false.
    /// Null means no length preference.
    /// </summary>
    public bool? PreferShorterSnippets { get; set; }

    /// <summary>
    /// Language filter. When set, only snippets in this language are selected.
    /// Overrides the language dropdown in settings.
    /// Null means use whatever language is currently selected.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Topic/tag filter. When set, prefer snippets with this topic.
    /// Null means no topic preference.
    /// </summary>
    public string? FocusTopic { get; set; }

    // ────────────────────────────────────────────────────────
    //  Typing rules overrides — optional, inherit from defaults
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Whitespace strictness: "strict", "lenient", "normalize", or null.
    /// Null means use the setting from the Settings panel.
    /// </summary>
    public string? Whitespace { get; set; }

    /// <summary>
    /// Backspace mode: "always", "limited", "never", or null.
    /// Null means use the setting from the Settings panel.
    /// </summary>
    public string? Backspace { get; set; }

    /// <summary>
    /// Accuracy floor for XP (0-100), or null for the default.
    /// </summary>
    public double? AccuracyFloor { get; set; }

    // ────────────────────────────────────────────────────────
    //  Computed helpers
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the difficulty bias to a numeric offset.
    /// Returns 0 if the bias is unrecognized or null.
    /// </summary>
    [JsonIgnore]
    public int DifficultyOffset => DifficultyBias?.ToLowerInvariant() switch
    {
        "easier" => -1,
        "harder" => +1,
        "match" => 0,
        _ => 0
    };

    /// <summary>
    /// Whether this config has a difficulty bias override.
    /// </summary>
    [JsonIgnore]
    public bool HasDifficultyBias => !string.IsNullOrEmpty(DifficultyBias);

    /// <summary>
    /// Applies this config's typing rule overrides to a base set of rules.
    /// Only overrides fields that are explicitly set in the config.
    /// </summary>
    public TypingRules ApplyTo(TypingRules baseRules)
    {
        var rules = baseRules.Clone();

        if (!string.IsNullOrEmpty(Whitespace))
        {
            rules.WhitespaceStrictness = Whitespace.ToLowerInvariant() switch
            {
                "strict" => WhitespaceMode.Strict,
                "lenient" => WhitespaceMode.Lenient,
                "normalize" => WhitespaceMode.Normalize,
                _ => rules.WhitespaceStrictness
            };
        }

        if (!string.IsNullOrEmpty(Backspace))
        {
            rules.Backspace = Backspace.ToLowerInvariant() switch
            {
                "always" => BackspaceMode.Always,
                "limited" => BackspaceMode.Limited,
                "never" => BackspaceMode.Never,
                _ => rules.Backspace
            };
        }

        if (AccuracyFloor.HasValue)
        {
            rules.AccuracyFloorForXp = Math.Clamp(AccuracyFloor.Value, 0, 100);
        }

        return rules;
    }

    /// <summary>
    /// The default (built-in) config — no overrides, everything uses app defaults.
    /// </summary>
    public static PracticeConfig Default => new()
    {
        Name = "Default",
        Description = "Standard practice with adaptive difficulty",
        IsUserAuthored = false
    };
}
