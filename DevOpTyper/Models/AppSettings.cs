namespace DevOpTyper.Models;

/// <summary>
/// Application settings persisted across sessions.
/// </summary>
public sealed class AppSettings
{
    #region Language Settings

    /// <summary>
    /// Currently selected programming language.
    /// </summary>
    public string SelectedLanguage { get; set; } = "python";

    /// <summary>
    /// Additional languages the user wants to practice.
    /// </summary>
    public List<string> FavoriteLanguages { get; set; } = new() { "python", "csharp" };

    #endregion

    #region Audio Settings

    /// <summary>
    /// Whether ambient sounds are muted.
    /// </summary>
    public bool IsAmbientMuted { get; set; } = false;

    /// <summary>
    /// Whether keyboard sounds are muted.
    /// </summary>
    public bool IsKeyboardMuted { get; set; } = false;

    /// <summary>
    /// Whether UI sounds are muted.
    /// </summary>
    public bool IsUiMuted { get; set; } = false;

    /// <summary>
    /// Master mute toggle.
    /// </summary>
    public bool IsMasterMuted { get; set; } = false;

    /// <summary>
    /// Ambient sound volume (0.0 - 1.0).
    /// </summary>
    public double AmbientVolume { get; set; } = 0.5;

    /// <summary>
    /// Keyboard click volume (0.0 - 1.0).
    /// </summary>
    public double KeyboardVolume { get; set; } = 0.7;

    /// <summary>
    /// UI click volume (0.0 - 1.0).
    /// </summary>
    public double UiClickVolume { get; set; } = 0.6;

    /// <summary>
    /// Keyboard sound mode.
    /// </summary>
    public string KeyboardSoundMode { get; set; } = "EveryKey";

    /// <summary>
    /// Keyboard sound theme (Mechanical, Membrane, Thock, Clicky).
    /// </summary>
    public string KeyboardSoundTheme { get; set; } = "Mechanical";

    /// <summary>
    /// Selected ambient soundscape name.
    /// </summary>
    public string SelectedSoundscape { get; set; } = "Default";

    #endregion

    #region Typing Settings

    /// <summary>
    /// Whether hardcore mode is enabled (no backspace past errors).
    /// </summary>
    public bool HardcoreMode { get; set; } = false;

    /// <summary>
    /// Whether to auto-advance to next snippet after completion.
    /// </summary>
    public bool AutoAdvance { get; set; } = true;

    /// <summary>
    /// Delay in seconds before auto-advancing.
    /// </summary>
    public double AutoAdvanceDelay { get; set; } = 2.0;

    /// <summary>
    /// Whether to show the code completion overlay.
    /// </summary>
    public bool ShowCompletionOverlay { get; set; } = true;

    #endregion

    #region Display Settings

    /// <summary>
    /// Use high contrast theme.
    /// </summary>
    public bool HighContrast { get; set; } = false;

    /// <summary>
    /// Font size for code display.
    /// </summary>
    public double CodeFontSize { get; set; } = 16.0;

    /// <summary>
    /// Whether the sidebar is open.
    /// </summary>
    public bool SidebarOpen { get; set; } = false;

    /// <summary>
    /// Width of the sidebar in pixels.
    /// </summary>
    public double SidebarWidth { get; set; } = 300.0;

    /// <summary>
    /// Show line numbers in code display.
    /// </summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Show character position indicator.
    /// </summary>
    public bool ShowCharPosition { get; set; } = true;

    #endregion

    #region Accessibility Settings

    /// <summary>
    /// Reduced motion preference.
    /// </summary>
    public bool ReducedMotion { get; set; } = false;

    /// <summary>
    /// Disable all animations.
    /// </summary>
    public bool DisableAnimations { get; set; } = false;

    /// <summary>
    /// Animation speed multiplier.
    /// </summary>
    public double AnimationSpeedMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Large text mode.
    /// </summary>
    public bool LargeText { get; set; } = false;

    /// <summary>
    /// Text scale factor.
    /// </summary>
    public double TextScaleFactor { get; set; } = 1.0;

    /// <summary>
    /// Show enhanced focus indicators.
    /// </summary>
    public bool FocusIndicatorsEnabled { get; set; } = true;

    /// <summary>
    /// Screen reader optimization mode.
    /// </summary>
    public bool ScreenReaderMode { get; set; } = false;

    /// <summary>
    /// Extended timers for timed interactions.
    /// </summary>
    public bool ExtendedTimers { get; set; } = false;

    #endregion

    #region Snippet Settings

    /// <summary>
    /// Filter snippets by difficulty.
    /// </summary>
    public string DifficultyFilter { get; set; } = "all";

    /// <summary>
    /// Only show favorited snippets.
    /// </summary>
    public bool ShowFavoritesOnly { get; set; } = false;

    /// <summary>
    /// Use smart snippet selection.
    /// </summary>
    public bool SmartSnippetSelection { get; set; } = true;

    #endregion

    #region Statistics Display

    /// <summary>
    /// Show WPM in stats panel.
    /// </summary>
    public bool ShowWpm { get; set; } = true;

    /// <summary>
    /// Show accuracy in stats panel.
    /// </summary>
    public bool ShowAccuracy { get; set; } = true;

    /// <summary>
    /// Show error count in stats panel.
    /// </summary>
    public bool ShowErrorCount { get; set; } = true;

    /// <summary>
    /// Show elapsed time in stats panel.
    /// </summary>
    public bool ShowElapsedTime { get; set; } = true;

    #endregion

    /// <summary>
    /// Creates a copy of this settings object.
    /// </summary>
    public AppSettings Clone()
    {
        return new AppSettings
        {
            SelectedLanguage = SelectedLanguage,
            FavoriteLanguages = new List<string>(FavoriteLanguages),
            IsAmbientMuted = IsAmbientMuted,
            IsKeyboardMuted = IsKeyboardMuted,
            IsUiMuted = IsUiMuted,
            IsMasterMuted = IsMasterMuted,
            AmbientVolume = AmbientVolume,
            KeyboardVolume = KeyboardVolume,
            UiClickVolume = UiClickVolume,
            KeyboardSoundMode = KeyboardSoundMode,
            KeyboardSoundTheme = KeyboardSoundTheme,
            SelectedSoundscape = SelectedSoundscape,
            HardcoreMode = HardcoreMode,
            AutoAdvance = AutoAdvance,
            AutoAdvanceDelay = AutoAdvanceDelay,
            ShowCompletionOverlay = ShowCompletionOverlay,
            HighContrast = HighContrast,
            CodeFontSize = CodeFontSize,
            SidebarOpen = SidebarOpen,
            SidebarWidth = SidebarWidth,
            ShowLineNumbers = ShowLineNumbers,
            ShowCharPosition = ShowCharPosition,
            ReducedMotion = ReducedMotion,
            DisableAnimations = DisableAnimations,
            AnimationSpeedMultiplier = AnimationSpeedMultiplier,
            LargeText = LargeText,
            TextScaleFactor = TextScaleFactor,
            FocusIndicatorsEnabled = FocusIndicatorsEnabled,
            ScreenReaderMode = ScreenReaderMode,
            ExtendedTimers = ExtendedTimers,
            DifficultyFilter = DifficultyFilter,
            ShowFavoritesOnly = ShowFavoritesOnly,
            SmartSnippetSelection = SmartSnippetSelection,
            ShowWpm = ShowWpm,
            ShowAccuracy = ShowAccuracy,
            ShowErrorCount = ShowErrorCount,
            ShowElapsedTime = ShowElapsedTime
        };
    }
}
