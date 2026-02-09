namespace DevOpTyper.Models;

/// <summary>
/// Configurable rules for how typed text is compared to target text.
/// These rules let advanced users control strictness without code changes.
/// </summary>
public sealed class TypingRules
{
    /// <summary>
    /// How strictly whitespace is compared.
    /// </summary>
    public WhitespaceMode WhitespaceStrictness { get; set; } = WhitespaceMode.Lenient;

    /// <summary>
    /// How line endings are handled.
    /// </summary>
    public LineEndingMode LineEndings { get; set; } = LineEndingMode.Normalize;

    /// <summary>
    /// How trailing spaces on lines are handled.
    /// </summary>
    public TrailingSpaceMode TrailingSpaces { get; set; } = TrailingSpaceMode.Ignore;

    /// <summary>
    /// Whether backspace is allowed and how.
    /// </summary>
    public BackspaceMode Backspace { get; set; } = BackspaceMode.Always;

    /// <summary>
    /// Maximum number of corrections allowed per session (only applies when Backspace = Limited).
    /// </summary>
    public int MaxCorrections { get; set; } = 10;

    /// <summary>
    /// Whether adaptive difficulty is enabled (biases snippets toward weak areas).
    /// </summary>
    public bool AdaptiveDifficulty { get; set; } = true;

    /// <summary>
    /// Minimum accuracy to earn XP (0-100). Below this, XP = 0.
    /// </summary>
    public double AccuracyFloorForXp { get; set; } = 70.0;

    /// <summary>
    /// Applies the configured rules to normalize a text string for comparison.
    /// Both target and typed text should be passed through this before comparison.
    /// </summary>
    public string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";

        // Line endings
        text = LineEndings switch
        {
            LineEndingMode.Normalize => text.Replace("\r\n", "\n").Replace("\r", "\n"),
            LineEndingMode.Exact => text,
            _ => text
        };

        // Trailing spaces
        if (TrailingSpaces == TrailingSpaceMode.Ignore)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd(' ', '\t');
            }
            text = string.Join("\n", lines);
        }

        // Whitespace normalization
        if (WhitespaceStrictness == WhitespaceMode.Normalize)
        {
            // Convert tabs to spaces (4 spaces per tab)
            text = text.Replace("\t", "    ");
        }

        return text;
    }

    /// <summary>
    /// Creates a copy of these rules.
    /// </summary>
    public TypingRules Clone()
    {
        return new TypingRules
        {
            WhitespaceStrictness = WhitespaceStrictness,
            LineEndings = LineEndings,
            TrailingSpaces = TrailingSpaces,
            Backspace = Backspace,
            MaxCorrections = MaxCorrections,
            AdaptiveDifficulty = AdaptiveDifficulty,
            AccuracyFloorForXp = AccuracyFloorForXp
        };
    }

    /// <summary>
    /// Returns default "just works" rules for new users.
    /// </summary>
    public static TypingRules Default => new();
}

/// <summary>
/// How whitespace characters are compared.
/// </summary>
public enum WhitespaceMode
{
    /// <summary>Exact match required (spaces != tabs).</summary>
    Strict,

    /// <summary>Tabs converted to spaces before comparison.</summary>
    Lenient,

    /// <summary>Tabs normalized to 4 spaces, equivalent runs collapsed.</summary>
    Normalize
}

/// <summary>
/// How line endings are handled.
/// </summary>
public enum LineEndingMode
{
    /// <summary>All line endings normalized to \n before comparison.</summary>
    Normalize,

    /// <summary>Line endings must match exactly.</summary>
    Exact
}

/// <summary>
/// How trailing spaces on lines are handled.
/// </summary>
public enum TrailingSpaceMode
{
    /// <summary>Trailing spaces must match.</summary>
    Strict,

    /// <summary>Trailing spaces are stripped before comparison.</summary>
    Ignore
}

/// <summary>
/// How backspace/correction is handled.
/// </summary>
public enum BackspaceMode
{
    /// <summary>Backspace always allowed.</summary>
    Always,

    /// <summary>Limited number of corrections per session.</summary>
    Limited,

    /// <summary>No backspace allowed (hardcore).</summary>
    Never
}
