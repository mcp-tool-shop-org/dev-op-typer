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

    /// <summary>
    /// Typing comparison rules (whitespace, line endings, backspace, accuracy floor).
    /// </summary>
    public TypingRules TypingRules { get; set; } = new();

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

    #region Practice Preferences (v0.4.0)

    /// <summary>
    /// Whether to show intent chips above the Start button.
    /// Users can hide them if they don't find them useful.
    /// </summary>
    public bool ShowIntentChips { get; set; } = true;

    /// <summary>
    /// The user's default declared intent, auto-selected on app launch.
    /// Null = no default (user starts with no intent selected).
    /// </summary>
    public UserIntent? DefaultIntent { get; set; }

    /// <summary>
    /// Free-text practice note — a place for the user to write
    /// what they're working on today or this week. Purely reflective,
    /// never read by the system. Max 200 chars.
    /// </summary>
    public string? PracticeNote { get; set; }

    /// <summary>
    /// Active focus area for snippet selection.
    /// When set, snippet selection preferentially includes snippets
    /// that touch this area (e.g., "brackets", "loops", "operators").
    /// Null = no focus — standard selection applies.
    /// The user can clear this at any time. It never locks them in.
    /// </summary>
    public string? FocusArea { get; set; }

    /// <summary>
    /// Whether to show practice suggestions in the stats panel.
    /// Users who don't want the system offering suggestions can turn
    /// them off entirely — no penalty, no nagging.
    /// </summary>
    public bool ShowSuggestions { get; set; } = true;

    /// <summary>
    /// Selected practice configuration name.
    /// "Default" uses standard adaptive behavior.
    /// User-authored configs override specific session parameters.
    /// </summary>
    public string SelectedPracticeConfig { get; set; } = "Default";

    #endregion

    #region Teaching Settings (v0.8.0)

    /// <summary>
    /// Whether to show scaffold hints on snippets.
    /// Scaffolds are optional learning aids that fade with demonstrated competence.
    /// Default: true. Disabling hides scaffold hints with zero other impact.
    /// </summary>
    public bool ShowScaffolds { get; set; } = true;

    /// <summary>
    /// Whether to show alternative demonstrations on snippets.
    /// Demonstrations show different valid approaches to the same problem.
    /// Default: true. Disabling hides demonstration panels with zero other impact.
    /// </summary>
    public bool ShowDemonstrations { get; set; } = true;

    /// <summary>
    /// Whether to show guidance notes from collective experience.
    /// Guidance notes are contextual observations — always dismissible.
    /// Default: true. Disabling hides all guidance with zero other impact.
    /// </summary>
    public bool ShowGuidance { get; set; } = true;

    /// <summary>
    /// Whether to show skill depth layers on snippets.
    /// Layers offer different depths ("Essentials", "Deeper", "Advanced").
    /// Default: true. Disabling hides all layers with zero other impact.
    /// </summary>
    public bool ShowSkillLayers { get; set; } = true;

    #endregion

    #region Community Settings (v0.7.0)

    /// <summary>
    /// Whether to show community signal hints (typical WPM, common difficulties).
    /// Default: true. Disabling hides all signal-derived hints with zero other
    /// impact — no penalty, no nagging, no behavior change.
    /// </summary>
    public bool ShowCommunitySignals { get; set; } = true;

    #endregion

    #region Signal Policy (v1.0.0)

    /// <summary>
    /// Controls whether learning signals influence practice behavior.
    /// All flags default to false — with Guided Mode off, behavior is
    /// identical to v0.9. User must explicitly opt in.
    /// </summary>
    public SignalPolicy SignalPolicy { get; set; } = new();

    /// <summary>
    /// Whether the one-time Guided Mode onboarding hint has been shown.
    /// Prevents nagging — shown once when enough weakness data exists.
    /// </summary>
    public bool GuidedModeHintShown { get; set; } = false;

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
            TypingRules = TypingRules.Clone(),
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
            ShowElapsedTime = ShowElapsedTime,
            ShowIntentChips = ShowIntentChips,
            DefaultIntent = DefaultIntent,
            PracticeNote = PracticeNote,
            FocusArea = FocusArea,
            ShowSuggestions = ShowSuggestions,
            SelectedPracticeConfig = SelectedPracticeConfig,
            ShowCommunitySignals = ShowCommunitySignals,
            ShowScaffolds = ShowScaffolds,
            ShowDemonstrations = ShowDemonstrations,
            ShowGuidance = ShowGuidance,
            ShowSkillLayers = ShowSkillLayers,
            SignalPolicy = SignalPolicy.Clone(),
            GuidedModeHintShown = GuidedModeHintShown
        };
    }
}
