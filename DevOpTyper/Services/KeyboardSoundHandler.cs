namespace DevOpTyper.Services;

/// <summary>
/// Handles keyboard typing sounds with configurable settings.
/// Supports various click modes: every keystroke, words only, or disabled.
/// </summary>
public sealed class KeyboardSoundHandler
{
    private readonly AudioService _audioService;

    // Mode settings
    private KeyboardSoundMode _mode = KeyboardSoundMode.EveryKey;
    private bool _playOnError = true;
    private bool _playOnCorrection = true;

    // Tracking state for word-mode
    private string _previousText = "";

    // Rate limiting to prevent sound spam
    private DateTime _lastKeySound = DateTime.MinValue;
    private readonly TimeSpan _minKeyInterval = TimeSpan.FromMilliseconds(15);

    public KeyboardSoundHandler(AudioService audioService)
    {
        _audioService = audioService;
    }

    // Public properties
    public KeyboardSoundMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public bool PlayOnError
    {
        get => _playOnError;
        set => _playOnError = value;
    }

    public bool PlayOnCorrection
    {
        get => _playOnCorrection;
        set => _playOnCorrection = value;
    }

    /// <summary>
    /// Call when user types. Handles sound based on current mode.
    /// </summary>
    public void OnTextChanged(string newText, bool hasError = false)
    {
        if (_mode == KeyboardSoundMode.Disabled) return;

        var now = DateTime.UtcNow;

        // Rate limiting
        if (now - _lastKeySound < _minKeyInterval)
        {
            _previousText = newText;
            return;
        }

        bool shouldPlay = false;

        switch (_mode)
        {
            case KeyboardSoundMode.EveryKey:
                // Play on any text change
                if (newText != _previousText)
                {
                    shouldPlay = true;
                }
                break;

            case KeyboardSoundMode.WordsOnly:
                // Play only at word boundaries (space, newline, punctuation)
                shouldPlay = IsAtWordBoundary(newText);
                break;

            case KeyboardSoundMode.ErrorsOnly:
                // Play only when there's an error
                shouldPlay = hasError && _playOnError;
                break;
        }

        if (shouldPlay)
        {
            _audioService.PlayKeyClick();
            _lastKeySound = now;
        }

        _previousText = newText;
    }

    /// <summary>
    /// Call when user makes an error.
    /// </summary>
    public void OnError()
    {
        if (_playOnError && _mode != KeyboardSoundMode.Disabled)
        {
            _audioService.PlayError();
        }
    }

    /// <summary>
    /// Call when user corrects an error (backspace).
    /// </summary>
    public void OnCorrection()
    {
        if (_playOnCorrection && _mode != KeyboardSoundMode.Disabled)
        {
            // Softer click for corrections
            _audioService.PlayKeyClick();
        }
    }

    /// <summary>
    /// Call when session completes successfully.
    /// </summary>
    public void OnSessionComplete()
    {
        _audioService.PlaySuccess();
    }

    /// <summary>
    /// Reset tracking state for new session.
    /// </summary>
    public void Reset()
    {
        _previousText = "";
        _lastKeySound = DateTime.MinValue;
    }

    private bool IsAtWordBoundary(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (text.Length <= _previousText.Length) return false;

        // Check if last typed character is a word boundary
        var lastChar = text[^1];
        return char.IsWhiteSpace(lastChar) || IsPunctuation(lastChar);
    }

    private static bool IsPunctuation(char c)
    {
        return c is '.' or ',' or ';' or ':' or '!' or '?' 
            or '(' or ')' or '{' or '}' or '[' or ']'
            or '"' or '\'' or '`';
    }
}

/// <summary>
/// Keyboard sound playback modes.
/// </summary>
public enum KeyboardSoundMode
{
    /// <summary>No keyboard sounds.</summary>
    Disabled,

    /// <summary>Play sound on every keystroke.</summary>
    EveryKey,

    /// <summary>Play sound only at word boundaries.</summary>
    WordsOnly,

    /// <summary>Play sound only on errors.</summary>
    ErrorsOnly
}
