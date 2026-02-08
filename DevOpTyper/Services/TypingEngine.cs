using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Core typing engine that manages the typing session and tracks results.
/// Wraps SessionState and provides events for UI binding.
/// </summary>
public sealed class TypingEngine
{
    private readonly SessionState _session = new();
    private readonly CharDiffAnalyzer _diffAnalyzer = new();
    private readonly HardcoreModeEnforcer _hardcoreEnforcer = new();
    private Snippet? _currentSnippet;
    private string _lastTyped = "";
    private CharDiff[] _currentDiff = Array.Empty<CharDiff>();

    public event EventHandler<TypingResultEventArgs>? SessionCompleted;
    public event EventHandler? SessionStarted;
    public event EventHandler<TypingProgressEventArgs>? ProgressUpdated;
    public event EventHandler<CharDiff[]>? DiffUpdated;
    public event EventHandler<string>? TextCorrected;

    /// <summary>
    /// Gets whether a typing session is currently active.
    /// </summary>
    public bool IsRunning => _session.IsRunning;

    /// <summary>
    /// Gets whether the current session is complete.
    /// </summary>
    public bool IsComplete => _session.IsComplete;

    /// <summary>
    /// Gets the current snippet being typed.
    /// </summary>
    public Snippet? CurrentSnippet => _currentSnippet;

    /// <summary>
    /// Gets the current live WPM.
    /// </summary>
    public double LiveWpm => _session.LiveWpm;

    /// <summary>
    /// Gets the current live accuracy percentage.
    /// </summary>
    public double LiveAccuracy => _session.LiveAccuracy;

    /// <summary>
    /// Gets the current error count.
    /// </summary>
    public int ErrorCount => _session.ErrorCount;

    /// <summary>
    /// Gets XP earned this session.
    /// </summary>
    public int XpEarned => _session.XpEarned;

    /// <summary>
    /// Gets the current character diff array.
    /// </summary>
    public CharDiff[] CurrentDiff => _currentDiff;

    /// <summary>
    /// Gets the index of the first error, or -1 if none.
    /// </summary>
    public int FirstErrorIndex => _diffAnalyzer.FirstErrorIndex(_currentDiff);

    /// <summary>
    /// Gets the hardcore mode enforcer for status messages.
    /// </summary>
    public HardcoreModeEnforcer HardcoreEnforcer => _hardcoreEnforcer;

    /// <summary>
    /// Starts a new typing session with the given snippet.
    /// </summary>
    public void StartSession(Snippet snippet, bool hardcoreMode = false)
    {
        _currentSnippet = snippet ?? throw new ArgumentNullException(nameof(snippet));
        _lastTyped = "";
        _currentDiff = _diffAnalyzer.ComputeDiff(snippet.Code ?? "", "");
        _hardcoreEnforcer.Initialize(snippet.Code ?? "", hardcoreMode);
        _session.Start(snippet.Code ?? "");
        SessionStarted?.Invoke(this, EventArgs.Empty);
        DiffUpdated?.Invoke(this, _currentDiff);
    }

    /// <summary>
    /// Updates the session with the user's current typed text.
    /// </summary>
    /// <param name="typed">The text the user has typed so far.</param>
    /// <param name="hardcoreMode">Whether hardcore mode is enabled.</param>
    /// <returns>The validated text (may be corrected in hardcore mode).</returns>
    public string UpdateTypedText(string typed, bool hardcoreMode = false)
    {
        if (!IsRunning) return typed ?? "";

        typed ??= "";

        // Apply hardcore mode enforcement
        var correctedText = _hardcoreEnforcer.ValidateAndCorrect(typed);
        bool wasCorrected = correctedText != typed;
        
        if (wasCorrected)
        {
            TextCorrected?.Invoke(this, correctedText);
            typed = correctedText;
        }

        _lastTyped = typed;

        // Compute diff
        var target = _currentSnippet?.Code ?? "";
        _currentDiff = _diffAnalyzer.ComputeDiff(target, typed);
        
        _session.Update(typed, hardcoreMode);

        var progress = new TypingProgressEventArgs
        {
            Wpm = LiveWpm,
            Accuracy = LiveAccuracy,
            ErrorCount = ErrorCount,
            TypedLength = typed.Length,
            TargetLength = target.Length,
            FirstErrorIndex = _diffAnalyzer.FirstErrorIndex(_currentDiff),
            Diff = _currentDiff,
            HardcoreMessage = _hardcoreEnforcer.GetStatusMessage()
        };

        ProgressUpdated?.Invoke(this, progress);
        DiffUpdated?.Invoke(this, _currentDiff);

        if (_session.IsComplete)
        {
            OnSessionCompleted();
        }

        return typed;
    }

    /// <summary>
    /// Cancels the current session without recording results.
    /// </summary>
    public void CancelSession()
    {
        if (!IsRunning) return;
        _session.Cancel();
        _currentSnippet = null;
    }

    /// <summary>
    /// Resets the engine for a new session.
    /// </summary>
    public void Reset()
    {
        CancelSession();
    }

    private void OnSessionCompleted()
    {
        var result = new TypingResultEventArgs
        {
            Snippet = _currentSnippet,
            FinalWpm = LiveWpm,
            FinalAccuracy = LiveAccuracy,
            ErrorCount = ErrorCount,
            XpEarned = XpEarned
        };

        SessionCompleted?.Invoke(this, result);
    }
}

/// <summary>
/// Event args for typing progress updates.
/// </summary>
public class TypingProgressEventArgs : EventArgs
{
    public double Wpm { get; init; }
    public double Accuracy { get; init; }
    public int ErrorCount { get; init; }
    public int TypedLength { get; init; }
    public int TargetLength { get; init; }
    public int FirstErrorIndex { get; init; } = -1;
    public CharDiff[] Diff { get; init; } = Array.Empty<CharDiff>();
    public string HardcoreMessage { get; init; } = "";

    public double CompletionPercentage => TargetLength > 0
        ? 100.0 * TypedLength / TargetLength
        : 0;

    public bool HasErrors => ErrorCount > 0;
    public bool IsHardcoreActive => !string.IsNullOrEmpty(HardcoreMessage);
}

/// <summary>
/// Event args for session completion.
/// </summary>
public class TypingResultEventArgs : EventArgs
{
    public Snippet? Snippet { get; init; }
    public double FinalWpm { get; init; }
    public double FinalAccuracy { get; init; }
    public int ErrorCount { get; init; }
    public int XpEarned { get; init; }
}
