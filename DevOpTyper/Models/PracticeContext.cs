namespace DevOpTyper.Models;

/// <summary>
/// Optional context describing the intent behind a practice session.
/// All fields are nullable — the user can practice without context,
/// and the system can attach context without the user's involvement.
///
/// v0.3.0: This is a data-only primitive. No behavior is attached yet.
/// v0.4.0: Added UserIntent — a user-declared label describing why they're practicing.
///         UserIntent is descriptive, not evaluative. It has no impact on scoring,
///         difficulty, or suggestions. It exists solely for the user's own awareness
///         and for optional retrospective correlation.
/// </summary>
public sealed class PracticeContext
{
    /// <summary>
    /// Why this session was started (system-assigned).
    /// Null = user pressed Start with no particular intent.
    /// </summary>
    public PracticeIntent? Intent { get; set; }

    /// <summary>
    /// User-declared practice intent — what the user says they're doing.
    /// Null = user didn't declare an intent (most sessions).
    /// This is purely descriptive — it never affects scoring, difficulty, or suggestions.
    /// </summary>
    public UserIntent? DeclaredIntent { get; set; }

    /// <summary>
    /// What the session focuses on, if anything.
    /// Examples: "brackets", "python:loops", a specific weak char.
    /// Null = no particular focus.
    /// </summary>
    public string? Focus { get; set; }

    /// <summary>
    /// Optional tag for grouping related sessions.
    /// Null = standalone session.
    /// </summary>
    public string? GroupTag { get; set; }

    /// <summary>
    /// Whether the system chose this snippet (true) or the user did (false/null).
    /// Helps distinguish deliberate practice from system suggestions.
    /// </summary>
    public bool? SystemSelected { get; set; }

    /// <summary>
    /// The difficulty at the time of session start, before any adaptation.
    /// Captured so longitudinal analysis can track difficulty drift.
    /// </summary>
    public int? EffectiveDifficulty { get; set; }

    /// <summary>
    /// The user's rating in this language at session start.
    /// Captured as a snapshot so trend analysis doesn't need to reconstruct history.
    /// </summary>
    public int? RatingAtStart { get; set; }

    /// <summary>
    /// Creates a minimal context for a standard (unintentional) session.
    /// </summary>
    public static PracticeContext Default() => new()
    {
        Intent = PracticeIntent.Freeform,
        SystemSelected = true
    };

    /// <summary>
    /// Creates a context for weakness-targeted practice.
    /// </summary>
    public static PracticeContext ForWeakness(string focus) => new()
    {
        Intent = PracticeIntent.WeaknessTarget,
        Focus = focus,
        SystemSelected = true
    };

    /// <summary>
    /// Creates a context for user-initiated repeat practice.
    /// </summary>
    public static PracticeContext ForRepeat() => new()
    {
        Intent = PracticeIntent.Repeat,
        SystemSelected = false
    };
}

/// <summary>
/// Why a session was started. Describes intent, not outcome.
/// </summary>
public enum PracticeIntent
{
    /// <summary>
    /// No particular intent — just practicing.
    /// </summary>
    Freeform = 0,

    /// <summary>
    /// Targeting a specific weakness (char, group, or topic).
    /// </summary>
    WeaknessTarget = 1,

    /// <summary>
    /// Repeating a previously completed snippet.
    /// </summary>
    Repeat = 2,

    /// <summary>
    /// Exploring a new language or difficulty level.
    /// </summary>
    Exploration = 3,

    /// <summary>
    /// Warmup session (short, easy).
    /// </summary>
    Warmup = 4
}

/// <summary>
/// User-declared practice intent — what the user says they're doing.
/// These labels are descriptive, not evaluative. They have zero impact
/// on scoring, difficulty, snippet selection, or suggestions.
/// The user can select one before starting, or leave it blank.
/// </summary>
public enum UserIntent
{
    /// <summary>
    /// Concentrated practice on a specific area.
    /// "I want to work on something specific."
    /// </summary>
    Focus = 0,

    /// <summary>
    /// Pushing beyond current comfort level.
    /// "I want to stretch myself."
    /// </summary>
    Challenge = 1,

    /// <summary>
    /// Keeping existing skills sharp.
    /// "I want to stay in shape."
    /// </summary>
    Maintenance = 2,

    /// <summary>
    /// Trying something new or unfamiliar.
    /// "I want to see what's out there."
    /// </summary>
    Exploration = 3
}
