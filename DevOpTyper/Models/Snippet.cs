using System.Text.Json.Serialization;

namespace DevOpTyper.Models;

/// <summary>
/// Represents a code snippet for typing practice.
/// </summary>
public sealed class Snippet
{
    /// <summary>
    /// Unique identifier for the snippet.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Programming language (e.g., "python", "javascript", "csharp").
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>
    /// Difficulty level from 1 (easiest) to 7 (hardest).
    /// Derived deterministically from code metrics (lines, symbol density,
    /// indent depth) unless overridden by authored metadata.
    /// </summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>
    /// Display title for the snippet.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// The actual code to type.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Topics/tags for categorization (e.g., "loops", "functions", "async").
    /// </summary>
    public string[] Topics { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Educational explanation lines for the snippet.
    /// </summary>
    public string[] Explain { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional scaffold hints for this snippet (v0.8.0).
    /// Short, supportive cues that help the user notice patterns
    /// in the code ("Watch the bracket alignment", "Semicolons at line ends").
    /// Scaffolds fade with demonstrated competence — they are aids, not prerequisites.
    /// </summary>
    public string[] Scaffolds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Alternative approaches to the same problem (v0.8.0).
    /// Multiple demonstrations coexist without ranking or recommending.
    /// No authorship metadata. Labels describe the approach, never the author.
    /// </summary>
    public Demonstration[] Demonstrations { get; set; } = Array.Empty<Demonstration>();

    /// <summary>
    /// Depth tiers for this snippet (v0.8.0).
    /// Layers offer different depths on the same snippet — "Essentials",
    /// "Deeper", "Advanced". Labels describe the content's depth, not the user's level.
    /// All layers are accessible to all users at all times.
    /// </summary>
    public SkillLayer[] Layers { get; set; } = Array.Empty<SkillLayer>();

    /// <summary>
    /// Multiple perspectives on the snippet (v0.7.0).
    /// Each perspective offers a different viewpoint — no hierarchy,
    /// no single "correct" explanation enforced. Labels describe the
    /// perspective's focus, never its author.
    /// </summary>
    public ExplanationSet[] Perspectives { get; set; } = Array.Empty<ExplanationSet>();

    /// <summary>
    /// Whether this snippet was authored by the user (vs built-in).
    /// User-authored snippets behave identically to built-in ones during
    /// practice — same scoring, same XP, same session records.
    /// This flag exists only so the UI can optionally distinguish origin.
    /// </summary>
    [JsonIgnore]
    public bool IsUserAuthored { get; set; }

    /// <summary>
    /// Special characters used in this snippet (computed).
    /// </summary>
    [JsonIgnore]
    public HashSet<char> SpecialChars => ComputeSpecialChars();

    /// <summary>
    /// Character count.
    /// </summary>
    [JsonIgnore]
    public int CharCount => Code?.Length ?? 0;

    /// <summary>
    /// Estimated typing time in seconds based on 40 WPM average.
    /// </summary>
    [JsonIgnore]
    public int EstimatedSeconds => (int)Math.Ceiling(CharCount / 5.0 / 40.0 * 60.0);

    /// <summary>
    /// Gets the difficulty label.
    /// </summary>
    [JsonIgnore]
    public string DifficultyLabel => Difficulty switch
    {
        1 => "Trivial",
        2 => "Easy",
        3 => "Moderate",
        4 => "Intermediate",
        5 => "Challenging",
        6 => "Advanced",
        7 => "Expert",
        _ => "Unknown"
    };

    private HashSet<char> ComputeSpecialChars()
    {
        var specials = new HashSet<char>();
        if (string.IsNullOrEmpty(Code)) return specials;

        foreach (var c in Code)
        {
            if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))
            {
                specials.Add(c);
            }
        }
        return specials;
    }
}

/// <summary>
/// Represents a language track with metadata.
/// </summary>
public sealed class LanguageTrack
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "";
    public int SnippetCount { get; set; }
    public string[] AvailableDifficulties { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this track includes user-authored snippets.
    /// Display-only — never affects selection or scoring.
    /// </summary>
    public bool HasUserContent { get; set; }
}

/// <summary>
/// A named set of explanatory notes offering one perspective on a snippet.
/// Multiple perspectives can coexist without hierarchy.
///
/// Label describes the perspective's focus (e.g., "Beginner view",
/// "Performance notes", "Common pitfalls"). It is never an author name.
///
/// Authorship decoupling: ExplanationSet deliberately omits author/origin
/// fields. During import, PortableBundleService deserializes into this model,
/// which strips any non-schema fields (author, source, createdBy) from the
/// JSON. This ensures imported perspectives are indistinguishable from
/// locally authored ones — the idea stands on its own.
/// </summary>
public sealed class ExplanationSet
{
    /// <summary>
    /// Short label describing this perspective's focus.
    /// Examples: "Beginner view", "Why this pattern", "Common pitfalls".
    /// Never an author name — perspectives are about ideas, not people.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Explanatory notes for this perspective. Short, descriptive lines.
    /// </summary>
    public string[] Notes { get; set; } = Array.Empty<string>();
}

