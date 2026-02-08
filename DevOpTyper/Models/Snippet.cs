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
    /// Difficulty level from 1 (beginner) to 5 (expert).
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
        1 => "Beginner",
        2 => "Easy",
        3 => "Intermediate",
        4 => "Advanced",
        5 => "Expert",
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

