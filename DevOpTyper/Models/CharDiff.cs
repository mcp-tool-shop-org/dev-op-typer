namespace DevOpTyper.Models;

/// <summary>
/// Represents the state of a single character in the typed text.
/// </summary>
public enum CharState
{
    /// <summary>Not yet typed.</summary>
    Pending,
    
    /// <summary>Typed correctly.</summary>
    Correct,
    
    /// <summary>Typed incorrectly.</summary>
    Error,
    
    /// <summary>Extra character beyond target length.</summary>
    Extra
}

/// <summary>
/// Represents a character position with its diff state.
/// </summary>
public readonly record struct CharDiff(int Index, char Expected, char? Actual, CharState State)
{
    /// <summary>
    /// Whether this character has been typed.
    /// </summary>
    public bool IsTyped => State != CharState.Pending;

    /// <summary>
    /// Whether this character was typed correctly.
    /// </summary>
    public bool IsCorrect => State == CharState.Correct;
}

/// <summary>
/// Provides character-by-character diff analysis for typing comparison.
/// </summary>
public sealed class CharDiffAnalyzer
{
    /// <summary>
    /// Computes the diff between target text and typed text.
    /// </summary>
    /// <param name="target">The text to type (expected).</param>
    /// <param name="typed">The text that was typed (actual).</param>
    /// <returns>Array of character diffs.</returns>
    public CharDiff[] ComputeDiff(string target, string typed)
    {
        target ??= "";
        typed ??= "";

        int maxLen = Math.Max(target.Length, typed.Length);
        var results = new CharDiff[maxLen];

        for (int i = 0; i < maxLen; i++)
        {
            char? expected = i < target.Length ? target[i] : null;
            char? actual = i < typed.Length ? typed[i] : null;

            CharState state;
            if (actual == null)
            {
                state = CharState.Pending;
            }
            else if (expected == null)
            {
                state = CharState.Extra;
            }
            else if (actual == expected)
            {
                state = CharState.Correct;
            }
            else
            {
                state = CharState.Error;
            }

            results[i] = new CharDiff(i, expected ?? ' ', actual, state);
        }

        return results;
    }

    /// <summary>
    /// Counts errors in the diff.
    /// </summary>
    public int CountErrors(CharDiff[] diff)
    {
        int count = 0;
        foreach (var d in diff)
        {
            if (d.State == CharState.Error || d.State == CharState.Extra)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Gets the index of the first error, or -1 if none.
    /// </summary>
    public int FirstErrorIndex(CharDiff[] diff)
    {
        for (int i = 0; i < diff.Length; i++)
        {
            if (diff[i].State == CharState.Error || diff[i].State == CharState.Extra)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Gets the index of the current cursor position (first pending or end).
    /// </summary>
    public int CursorPosition(CharDiff[] diff)
    {
        for (int i = 0; i < diff.Length; i++)
        {
            if (diff[i].State == CharState.Pending)
                return i;
        }
        return diff.Length;
    }
}
