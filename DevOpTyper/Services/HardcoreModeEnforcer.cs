using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Enforces hardcore mode rules: no backspace past errors, must fix sequentially.
/// </summary>
public sealed class HardcoreModeEnforcer
{
    private readonly CharDiffAnalyzer _diffAnalyzer = new();
    private string _target = "";
    private string _lastValidTyped = "";
    private int _firstErrorIndex = -1;

    /// <summary>
    /// Gets whether hardcore mode is currently active.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the index of the first error that needs correction.
    /// </summary>
    public int FirstErrorIndex => _firstErrorIndex;

    /// <summary>
    /// Gets the minimum allowed text length (can't delete past this).
    /// </summary>
    public int MinimumLength { get; private set; }

    /// <summary>
    /// Initializes the enforcer for a new session.
    /// </summary>
    public void Initialize(string target, bool enabled)
    {
        _target = target ?? "";
        _lastValidTyped = "";
        _firstErrorIndex = -1;
        MinimumLength = 0;
        IsEnabled = enabled;
    }

    /// <summary>
    /// Validates and potentially modifies the typed text according to hardcore rules.
    /// </summary>
    /// <param name="typed">The text the user typed.</param>
    /// <returns>The validated text (may be modified if rules were violated).</returns>
    public string ValidateAndCorrect(string typed)
    {
        if (!IsEnabled)
        {
            _lastValidTyped = typed ?? "";
            return _lastValidTyped;
        }

        typed ??= "";

        // Compute current diff
        var diff = _diffAnalyzer.ComputeDiff(_target, typed);
        _firstErrorIndex = _diffAnalyzer.FirstErrorIndex(diff);

        // Find first error in the text
        int firstError = -1;
        for (int i = 0; i < diff.Length && i < typed.Length; i++)
        {
            if (diff[i].State == CharState.Error || diff[i].State == CharState.Extra)
            {
                firstError = i;
                break;
            }
        }

        // If there's an error, lock the minimum length to that position
        if (firstError >= 0)
        {
            MinimumLength = firstError + 1;
        }
        else
        {
            // No errors - minimum is whatever is correctly typed
            MinimumLength = Math.Min(typed.Length, _target.Length);
        }

        // Enforce: user cannot delete past the first error
        if (typed.Length < _lastValidTyped.Length && _firstErrorIndex >= 0)
        {
            // User is trying to delete
            if (typed.Length <= _firstErrorIndex)
            {
                // They're trying to delete past the first error - prevent it
                return _lastValidTyped;
            }
        }

        _lastValidTyped = typed;
        return typed;
    }

    /// <summary>
    /// Resets the enforcer state.
    /// </summary>
    public void Reset()
    {
        _target = "";
        _lastValidTyped = "";
        _firstErrorIndex = -1;
        MinimumLength = 0;
    }

    /// <summary>
    /// Gets a message describing the current hardcore mode state.
    /// </summary>
    public string GetStatusMessage()
    {
        if (!IsEnabled)
            return "";

        if (_firstErrorIndex >= 0)
            return $"Fix error at position {_firstErrorIndex + 1}";

        return "Hardcore: No backspace past errors";
    }
}
