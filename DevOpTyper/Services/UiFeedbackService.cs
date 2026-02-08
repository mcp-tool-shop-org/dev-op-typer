namespace DevOpTyper.Services;

/// <summary>
/// Provides UI feedback through sounds and visual cues.
/// </summary>
public sealed class UiFeedbackService
{
    private readonly AudioService _audioService;

    // Enabled states
    private bool _soundsEnabled = true;
    private bool _visualFeedbackEnabled = true;

    // Events for visual feedback handlers
    public event EventHandler<UiFeedbackEventArgs>? FeedbackRequested;

    public UiFeedbackService(AudioService audioService)
    {
        _audioService = audioService;
    }

    // Public properties
    public bool SoundsEnabled
    {
        get => _soundsEnabled;
        set => _soundsEnabled = value;
    }

    public bool VisualFeedbackEnabled
    {
        get => _visualFeedbackEnabled;
        set => _visualFeedbackEnabled = value;
    }

    #region Button and Control Feedback

    /// <summary>
    /// Call when a button is clicked.
    /// </summary>
    public void OnButtonClick()
    {
        if (_soundsEnabled)
        {
            _audioService.PlayUiClick();
        }

        RaiseFeedback(UiFeedbackType.ButtonClick);
    }

    /// <summary>
    /// Call when a toggle switch changes state.
    /// </summary>
    public void OnToggleChanged(bool newState)
    {
        if (_soundsEnabled)
        {
            _audioService.PlayUiClick();
        }

        RaiseFeedback(newState ? UiFeedbackType.ToggleOn : UiFeedbackType.ToggleOff);
    }

    /// <summary>
    /// Call when a slider value changes (debounced).
    /// </summary>
    public void OnSliderChanged()
    {
        // Slider sounds are typically softer or omitted
        RaiseFeedback(UiFeedbackType.SliderMoved);
    }

    /// <summary>
    /// Call when a list item is selected.
    /// </summary>
    public void OnListItemSelected()
    {
        if (_soundsEnabled)
        {
            _audioService.PlayUiClick();
        }

        RaiseFeedback(UiFeedbackType.ListItemSelected);
    }

    /// <summary>
    /// Call when navigation occurs.
    /// </summary>
    public void OnNavigate()
    {
        RaiseFeedback(UiFeedbackType.Navigate);
    }

    #endregion

    #region State Feedback

    /// <summary>
    /// Call when an operation succeeds.
    /// </summary>
    public void OnSuccess(string? message = null)
    {
        if (_soundsEnabled)
        {
            _audioService.PlaySuccess();
        }

        RaiseFeedback(UiFeedbackType.Success, message);
    }

    /// <summary>
    /// Call when an error occurs.
    /// </summary>
    public void OnError(string? message = null)
    {
        if (_soundsEnabled)
        {
            _audioService.PlayError();
        }

        RaiseFeedback(UiFeedbackType.Error, message);
    }

    /// <summary>
    /// Call when a warning should be shown.
    /// </summary>
    public void OnWarning(string? message = null)
    {
        RaiseFeedback(UiFeedbackType.Warning, message);
    }

    /// <summary>
    /// Call when information should be shown.
    /// </summary>
    public void OnInfo(string? message = null)
    {
        RaiseFeedback(UiFeedbackType.Info, message);
    }

    #endregion

    #region Session Feedback

    /// <summary>
    /// Call when a typing session starts.
    /// </summary>
    public void OnSessionStart()
    {
        RaiseFeedback(UiFeedbackType.SessionStart);
    }

    /// <summary>
    /// Call when a typing session completes.
    /// </summary>
    public void OnSessionComplete(bool perfect)
    {
        if (_soundsEnabled)
        {
            _audioService.PlaySuccess();
        }

        RaiseFeedback(perfect ? UiFeedbackType.PerfectComplete : UiFeedbackType.SessionComplete);
    }

    /// <summary>
    /// Call when a milestone is reached (e.g., 50% complete).
    /// </summary>
    public void OnMilestoneReached(int percentage)
    {
        RaiseFeedback(UiFeedbackType.MilestoneReached, percentage.ToString());
    }

    #endregion

    #region Progress Feedback

    /// <summary>
    /// Call during loading operations.
    /// </summary>
    public void OnLoadingStart()
    {
        RaiseFeedback(UiFeedbackType.LoadingStart);
    }

    /// <summary>
    /// Call when loading completes.
    /// </summary>
    public void OnLoadingComplete()
    {
        RaiseFeedback(UiFeedbackType.LoadingComplete);
    }

    #endregion

    private void RaiseFeedback(UiFeedbackType type, string? message = null)
    {
        if (_visualFeedbackEnabled)
        {
            FeedbackRequested?.Invoke(this, new UiFeedbackEventArgs(type, message));
        }
    }
}

/// <summary>
/// Types of UI feedback events.
/// </summary>
public enum UiFeedbackType
{
    ButtonClick,
    ToggleOn,
    ToggleOff,
    SliderMoved,
    ListItemSelected,
    Navigate,

    Success,
    Error,
    Warning,
    Info,

    SessionStart,
    SessionComplete,
    PerfectComplete,
    MilestoneReached,

    LoadingStart,
    LoadingComplete
}

/// <summary>
/// Event args for UI feedback events.
/// </summary>
public class UiFeedbackEventArgs : EventArgs
{
    public UiFeedbackType FeedbackType { get; }
    public string? Message { get; }

    public UiFeedbackEventArgs(UiFeedbackType feedbackType, string? message = null)
    {
        FeedbackType = feedbackType;
        Message = message;
    }
}
