namespace DevOpTyper.Services;

/// <summary>
/// Manages accessibility settings including reduced motion and sensory preferences.
/// </summary>
public sealed class AccessibilitySettings
{
    // Singleton instance
    private static AccessibilitySettings? _instance;
    public static AccessibilitySettings Instance => _instance ??= new AccessibilitySettings();

    // Motion settings
    private bool _reducedMotion;
    private bool _disableAnimations;
    private double _animationSpeedMultiplier = 1.0;

    // Visual settings
    private bool _highContrast;
    private bool _largeText;
    private double _textScaleFactor = 1.0;
    private bool _focusIndicatorsEnabled = true;

    // Audio settings
    private bool _screenReaderMode;
    private bool _disableSounds;
    private bool _disableAmbientSounds;

    // Timing settings
    private bool _extendedTimers;
    private double _timerMultiplier = 1.0;

    // Events
    public event EventHandler<AccessibilityChangedEventArgs>? SettingsChanged;

    private AccessibilitySettings()
    {
        DetectSystemPreferences();
    }

    #region Motion Properties

    /// <summary>
    /// When true, minimize or disable animations.
    /// </summary>
    public bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (_reducedMotion == value) return;
            _reducedMotion = value;
            RaiseSettingsChanged(nameof(ReducedMotion));
        }
    }

    /// <summary>
    /// When true, completely disable all animations.
    /// </summary>
    public bool DisableAnimations
    {
        get => _disableAnimations;
        set
        {
            if (_disableAnimations == value) return;
            _disableAnimations = value;
            RaiseSettingsChanged(nameof(DisableAnimations));
        }
    }

    /// <summary>
    /// Speed multiplier for animations (0.5 = half speed, 2.0 = double speed).
    /// </summary>
    public double AnimationSpeedMultiplier
    {
        get => _animationSpeedMultiplier;
        set
        {
            var clamped = Math.Clamp(value, 0.1, 5.0);
            if (Math.Abs(_animationSpeedMultiplier - clamped) < 0.001) return;
            _animationSpeedMultiplier = clamped;
            RaiseSettingsChanged(nameof(AnimationSpeedMultiplier));
        }
    }

    /// <summary>
    /// Returns effective animation duration based on settings.
    /// </summary>
    public TimeSpan GetEffectiveDuration(TimeSpan baseDuration)
    {
        if (_disableAnimations) return TimeSpan.Zero;
        if (_reducedMotion) return TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds * 0.25);
        return TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds / _animationSpeedMultiplier);
    }

    /// <summary>
    /// Returns true if animations should play.
    /// </summary>
    public bool ShouldAnimate => !_disableAnimations;

    /// <summary>
    /// Returns true if animations should be simplified.
    /// </summary>
    public bool ShouldSimplifyAnimations => _reducedMotion;

    #endregion

    #region Visual Properties

    /// <summary>
    /// When true, use high contrast theme.
    /// </summary>
    public bool HighContrast
    {
        get => _highContrast;
        set
        {
            if (_highContrast == value) return;
            _highContrast = value;
            RaiseSettingsChanged(nameof(HighContrast));
        }
    }

    /// <summary>
    /// When true, use larger text sizes.
    /// </summary>
    public bool LargeText
    {
        get => _largeText;
        set
        {
            if (_largeText == value) return;
            _largeText = value;
            RaiseSettingsChanged(nameof(LargeText));
        }
    }

    /// <summary>
    /// Text scale factor (1.0 = normal, 1.5 = 150% larger).
    /// </summary>
    public double TextScaleFactor
    {
        get => _textScaleFactor;
        set
        {
            var clamped = Math.Clamp(value, 0.5, 3.0);
            if (Math.Abs(_textScaleFactor - clamped) < 0.001) return;
            _textScaleFactor = clamped;
            RaiseSettingsChanged(nameof(TextScaleFactor));
        }
    }

    /// <summary>
    /// When true, show enhanced focus indicators.
    /// </summary>
    public bool FocusIndicatorsEnabled
    {
        get => _focusIndicatorsEnabled;
        set
        {
            if (_focusIndicatorsEnabled == value) return;
            _focusIndicatorsEnabled = value;
            RaiseSettingsChanged(nameof(FocusIndicatorsEnabled));
        }
    }

    /// <summary>
    /// Returns scaled font size.
    /// </summary>
    public double GetScaledFontSize(double baseFontSize)
    {
        var scaled = baseFontSize * _textScaleFactor;
        if (_largeText) scaled *= 1.25;
        return scaled;
    }

    #endregion

    #region Audio Properties

    /// <summary>
    /// When true, optimize for screen readers.
    /// </summary>
    public bool ScreenReaderMode
    {
        get => _screenReaderMode;
        set
        {
            if (_screenReaderMode == value) return;
            _screenReaderMode = value;
            RaiseSettingsChanged(nameof(ScreenReaderMode));
        }
    }

    /// <summary>
    /// When true, disable all sounds.
    /// </summary>
    public bool DisableSounds
    {
        get => _disableSounds;
        set
        {
            if (_disableSounds == value) return;
            _disableSounds = value;
            RaiseSettingsChanged(nameof(DisableSounds));
        }
    }

    /// <summary>
    /// When true, disable ambient/background sounds only.
    /// </summary>
    public bool DisableAmbientSounds
    {
        get => _disableAmbientSounds;
        set
        {
            if (_disableAmbientSounds == value) return;
            _disableAmbientSounds = value;
            RaiseSettingsChanged(nameof(DisableAmbientSounds));
        }
    }

    /// <summary>
    /// Returns true if sounds should play.
    /// </summary>
    public bool ShouldPlaySounds => !_disableSounds;

    /// <summary>
    /// Returns true if ambient sounds should play.
    /// </summary>
    public bool ShouldPlayAmbientSounds => !_disableSounds && !_disableAmbientSounds;

    #endregion

    #region Timing Properties

    /// <summary>
    /// When true, extend all timing-based interactions.
    /// </summary>
    public bool ExtendedTimers
    {
        get => _extendedTimers;
        set
        {
            if (_extendedTimers == value) return;
            _extendedTimers = value;
            RaiseSettingsChanged(nameof(ExtendedTimers));
        }
    }

    /// <summary>
    /// Timer multiplier for timed interactions.
    /// </summary>
    public double TimerMultiplier
    {
        get => _timerMultiplier;
        set
        {
            var clamped = Math.Clamp(value, 0.5, 5.0);
            if (Math.Abs(_timerMultiplier - clamped) < 0.001) return;
            _timerMultiplier = clamped;
            RaiseSettingsChanged(nameof(TimerMultiplier));
        }
    }

    /// <summary>
    /// Returns effective timer duration based on settings.
    /// </summary>
    public TimeSpan GetEffectiveTimerDuration(TimeSpan baseDuration)
    {
        var multiplier = _extendedTimers ? _timerMultiplier * 1.5 : _timerMultiplier;
        return TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds * multiplier);
    }

    #endregion

    #region System Detection

    /// <summary>
    /// Detect and apply system accessibility preferences.
    /// </summary>
    public void DetectSystemPreferences()
    {
        try
        {
            // Check Windows accessibility settings via UISettings
            var uiSettings = new Windows.UI.ViewManagement.UISettings();

            // Check for reduced motion preference via animation enabled
            // UISettings.AnimationsEnabled indicates if user prefers reduced motion
            _reducedMotion = !uiSettings.AnimationsEnabled;

            // Check for high contrast via AccessibilitySettings
            var accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
            _highContrast = accessibilitySettings.HighContrast;
        }
        catch
        {
            // Fall back to defaults if detection fails
        }
    }

    /// <summary>
    /// Reset all settings to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        _reducedMotion = false;
        _disableAnimations = false;
        _animationSpeedMultiplier = 1.0;
        _highContrast = false;
        _largeText = false;
        _textScaleFactor = 1.0;
        _focusIndicatorsEnabled = true;
        _screenReaderMode = false;
        _disableSounds = false;
        _disableAmbientSounds = false;
        _extendedTimers = false;
        _timerMultiplier = 1.0;

        RaiseSettingsChanged("*");
    }

    #endregion

    private void RaiseSettingsChanged(string settingName)
    {
        SettingsChanged?.Invoke(this, new AccessibilityChangedEventArgs(settingName));
    }
}

/// <summary>
/// Event args for accessibility settings changes.
/// </summary>
public class AccessibilityChangedEventArgs : EventArgs
{
    /// <summary>
    /// Name of the setting that changed, or "*" for all settings.
    /// </summary>
    public string SettingName { get; }

    public AccessibilityChangedEventArgs(string settingName)
    {
        SettingName = settingName;
    }
}
