using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Core typing engine that manages the typing session and tracks results.
/// Wraps SessionState and provides events for UI binding.
/// </summary>
public sealed class TypingEngine
{
    private readonly SessionState _session = new();
    private Snippet? _currentSnippet;

    public event EventHandler<TypingResultEventArgs>? SessionCompleted;
    public event EventHandler? SessionStarted;
    public event EventHandler<TypingProgressEventArgs>? ProgressUpdated;

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
    /// Starts a new typing session with the given snippet.
    /// </summary>
    public void StartSession(Snippet snippet)
    {
        _currentSnippet = snippet ?? throw new ArgumentNullException(nameof(snippet));
        _session.Start(snippet.Code ?? "");
        SessionStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the session with the user's current typed text.
    /// </summary>
    /// <param name="typed">The text the user has typed so far.</param>
    /// <param name="hardcoreMode">Whether hardcore mode is enabled.</param>
    public void UpdateTypedText(string typed, bool hardcoreMode = false)
    {
        if (!IsRunning) return;

        _session.Update(typed, hardcoreMode);

        ProgressUpdated?.Invoke(this, new TypingProgressEventArgs
        {
            Wpm = LiveWpm,
            Accuracy = LiveAccuracy,
            ErrorCount = ErrorCount,
            TypedLength = typed?.Length ?? 0,
            TargetLength = _currentSnippet?.Code?.Length ?? 0
        });

        if (_session.IsComplete)
        {
            OnSessionCompleted();
        }
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

    public double CompletionPercentage => TargetLength > 0
        ? 100.0 * TypedLength / TargetLength
        : 0;
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
