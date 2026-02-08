using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

public sealed partial class SettingsPanel : UserControl
{
    // Volume change events so MainWindow can update AudioService
    public event EventHandler<double>? AmbientVolumeChanged;
    public event EventHandler<double>? KeyboardVolumeChanged;
    public event EventHandler<double>? UiVolumeChanged;
    public event EventHandler<string>? KeyboardThemeChanged;
    public event EventHandler<string>? SoundscapeChanged;
    public event EventHandler<string>? PracticeConfigChanged;

    // Guards to prevent false events during programmatic population
    private bool _suppressThemeEvent;
    private bool _suppressSoundscapeEvent;
    private bool _suppressConfigEvent;

    public SettingsPanel()
    {
        InitializeComponent();

        // Wire slider value changes to events
        AmbientVolumeSlider.ValueChanged += (s, e) =>
            AmbientVolumeChanged?.Invoke(this, e.NewValue / 100.0);
        KeyboardVolumeSlider.ValueChanged += (s, e) =>
            KeyboardVolumeChanged?.Invoke(this, e.NewValue / 100.0);
        UiVolumeSlider.ValueChanged += (s, e) =>
            UiVolumeChanged?.Invoke(this, e.NewValue / 100.0);

        KeyboardThemeCombo.SelectionChanged += (s, e) =>
        {
            if (!_suppressThemeEvent)
                KeyboardThemeChanged?.Invoke(this, SelectedKeyboardTheme);
        };

        SoundscapeCombo.SelectionChanged += (s, e) =>
        {
            if (!_suppressSoundscapeEvent)
                SoundscapeChanged?.Invoke(this, SelectedSoundscape);
        };

        PracticeConfigCombo.SelectionChanged += (s, e) =>
        {
            if (!_suppressConfigEvent)
            {
                PracticeConfigChanged?.Invoke(this, SelectedPracticeConfigName);
                UpdateConfigDescription();
            }
        };

        // Accuracy floor slider label update
        AccuracyFloorSlider.ValueChanged += (s, e) =>
        {
            AccuracyFloorLabel.Text = $"{(int)e.NewValue}%";
        };

        // Clear focus area — always available, no penalty
        ClearFocusButton.Click += (_, _) =>
        {
            FocusAreaCombo.SelectedIndex = 0; // "None"
        };
    }

    /// <summary>
    /// Populates the keyboard theme dropdown from discovered themes.
    /// </summary>
    public void PopulateThemes(IReadOnlyList<string> themes, string selected)
    {
        _suppressThemeEvent = true;
        KeyboardThemeCombo.Items.Clear();
        int selectedIndex = 0;
        for (int i = 0; i < themes.Count; i++)
        {
            KeyboardThemeCombo.Items.Add(themes[i]);
            if (string.Equals(themes[i], selected, StringComparison.OrdinalIgnoreCase))
                selectedIndex = i;
        }
        KeyboardThemeCombo.SelectedIndex = themes.Count > 0 ? selectedIndex : -1;
        _suppressThemeEvent = false;
    }

    /// <summary>
    /// Populates the soundscape dropdown from discovered soundscapes.
    /// </summary>
    public void PopulateSoundscapes(IReadOnlyList<string> soundscapes, string selected)
    {
        _suppressSoundscapeEvent = true;
        SoundscapeCombo.Items.Clear();
        int selectedIndex = 0;
        for (int i = 0; i < soundscapes.Count; i++)
        {
            SoundscapeCombo.Items.Add(soundscapes[i]);
            if (string.Equals(soundscapes[i], selected, StringComparison.OrdinalIgnoreCase))
                selectedIndex = i;
        }
        SoundscapeCombo.SelectedIndex = soundscapes.Count > 0 ? selectedIndex : -1;
        _suppressSoundscapeEvent = false;
    }

    /// <summary>
    /// Loads typing rules values into the UI controls.
    /// Call during initialization to restore saved settings.
    /// </summary>
    public void LoadTypingRules(TypingRules rules)
    {

        WhitespaceCombo.SelectedIndex = rules.WhitespaceStrictness switch
        {
            WhitespaceMode.Strict => 0,
            WhitespaceMode.Lenient => 1,
            WhitespaceMode.Normalize => 2,
            _ => 1
        };

        LineEndingCombo.SelectedIndex = rules.LineEndings switch
        {
            LineEndingMode.Normalize => 0,
            LineEndingMode.Exact => 1,
            _ => 0
        };

        TrailingSpaceCombo.SelectedIndex = rules.TrailingSpaces switch
        {
            TrailingSpaceMode.Strict => 0,
            TrailingSpaceMode.Ignore => 1,
            _ => 1
        };

        BackspaceCombo.SelectedIndex = rules.Backspace switch
        {
            BackspaceMode.Always => 0,
            BackspaceMode.Limited => 1,
            BackspaceMode.Never => 2,
            _ => 0
        };

        AccuracyFloorSlider.Value = rules.AccuracyFloorForXp;
        AccuracyFloorLabel.Text = $"{(int)rules.AccuracyFloorForXp}%";

        AdaptiveDifficultyToggle.IsOn = rules.AdaptiveDifficulty;

    }

    /// <summary>
    /// Reads the current typing rules from the UI controls.
    /// </summary>
    public TypingRules GetTypingRules()
    {
        return new TypingRules
        {
            WhitespaceStrictness = WhitespaceCombo.SelectedIndex switch
            {
                0 => WhitespaceMode.Strict,
                1 => WhitespaceMode.Lenient,
                2 => WhitespaceMode.Normalize,
                _ => WhitespaceMode.Lenient
            },
            LineEndings = LineEndingCombo.SelectedIndex switch
            {
                0 => LineEndingMode.Normalize,
                1 => LineEndingMode.Exact,
                _ => LineEndingMode.Normalize
            },
            TrailingSpaces = TrailingSpaceCombo.SelectedIndex switch
            {
                0 => TrailingSpaceMode.Strict,
                1 => TrailingSpaceMode.Ignore,
                _ => TrailingSpaceMode.Ignore
            },
            Backspace = BackspaceCombo.SelectedIndex switch
            {
                0 => BackspaceMode.Always,
                1 => BackspaceMode.Limited,
                2 => BackspaceMode.Never,
                _ => BackspaceMode.Always
            },
            AccuracyFloorForXp = AccuracyFloorSlider.Value,
            AdaptiveDifficulty = AdaptiveDifficultyToggle.IsOn
        };
    }

    #region Properties — Core

    /// <summary>
    /// Gets the selected language.
    /// </summary>
    public string SelectedLanguage
    {
        get
        {
            var item = LanguageCombo.SelectedItem as ComboBoxItem;
            return item?.Content?.ToString()?.ToLowerInvariant() ?? "python";
        }
    }

    /// <summary>
    /// Gets the selected difficulty level (0-3).
    /// </summary>
    public int DifficultyLevel => DifficultyCombo.SelectedIndex;

    #endregion

    #region Properties — Audio

    /// <summary>
    /// Gets the ambient volume (0-1).
    /// </summary>
    public double AmbientVolume => AmbientVolumeSlider.Value / 100.0;

    /// <summary>
    /// Gets the keyboard sound volume (0-1).
    /// </summary>
    public double KeyboardVolume => KeyboardVolumeSlider.Value / 100.0;

    /// <summary>
    /// Gets the UI sound volume (0-1).
    /// </summary>
    public double UiVolume => UiVolumeSlider.Value / 100.0;

    /// <summary>
    /// Gets or sets the selected keyboard theme.
    /// </summary>
    public string SelectedKeyboardTheme
    {
        get => KeyboardThemeCombo.SelectedItem as string ?? "Mechanical";
        set
        {
            for (int i = 0; i < KeyboardThemeCombo.Items.Count; i++)
            {
                if (KeyboardThemeCombo.Items[i] is string name &&
                    string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    KeyboardThemeCombo.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected soundscape.
    /// </summary>
    public string SelectedSoundscape
    {
        get => SoundscapeCombo.SelectedItem as string ?? "Default";
        set
        {
            for (int i = 0; i < SoundscapeCombo.Items.Count; i++)
            {
                if (SoundscapeCombo.Items[i] is string name &&
                    string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    SoundscapeCombo.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    #endregion

    #region Properties — Practice Config

    /// <summary>
    /// Gets the selected practice config name.
    /// </summary>
    public string SelectedPracticeConfigName
    {
        get => PracticeConfigCombo.SelectedItem as string ?? "Default";
        set
        {
            for (int i = 0; i < PracticeConfigCombo.Items.Count; i++)
            {
                if (PracticeConfigCombo.Items[i] is string name &&
                    string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    PracticeConfigCombo.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Populates the practice config dropdown from discovered configs.
    /// </summary>
    public void PopulateConfigs(IReadOnlyList<string> configNames, string selected)
    {
        _suppressConfigEvent = true;
        PracticeConfigCombo.Items.Clear();
        int selectedIndex = 0;
        for (int i = 0; i < configNames.Count; i++)
        {
            PracticeConfigCombo.Items.Add(configNames[i]);
            if (string.Equals(configNames[i], selected, StringComparison.OrdinalIgnoreCase))
                selectedIndex = i;
        }
        PracticeConfigCombo.SelectedIndex = configNames.Count > 0 ? selectedIndex : -1;
        _suppressConfigEvent = false;
    }

    /// <summary>
    /// Updates the config description text below the dropdown.
    /// Screen readers announce the description politely when it changes.
    /// </summary>
    public void UpdateConfigDescription(string? description = null)
    {
        if (!string.IsNullOrEmpty(description))
        {
            PracticeConfigDescription.Text = description;
            PracticeConfigDescription.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                PracticeConfigDescription,
                $"Config description: {description}");
        }
        else
        {
            PracticeConfigDescription.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Shows config loading errors if any exist.
    /// Non-blocking — errors are informational only.
    /// </summary>
    public void UpdateConfigErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count > 0)
        {
            var errorText = string.Join("\n", errors.Take(3));
            PracticeConfigErrors.Text = errorText;
            PracticeConfigErrors.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                PracticeConfigErrors,
                $"Config loading issues: {errorText}");
        }
        else
        {
            PracticeConfigErrors.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    #endregion

    #region Properties — Gameplay & Rules

    /// <summary>
    /// Gets whether hardcore mode is enabled.
    /// </summary>
    public bool IsHardcoreMode => HardcoreModeToggle.IsOn;

    /// <summary>
    /// Gets whether adaptive difficulty is enabled.
    /// </summary>
    public bool IsAdaptiveDifficulty => AdaptiveDifficultyToggle.IsOn;

    /// <summary>
    /// Gets whether high contrast mode is enabled.
    /// </summary>
    public bool IsHighContrast => HighContrastToggle.IsOn;

    /// <summary>
    /// Gets whether reduced motion is enabled.
    /// </summary>
    public bool IsReducedMotion => ReducedMotionToggle.IsOn;

    #endregion

    #region Properties — Practice Preferences (v0.4.0)

    /// <summary>
    /// Gets whether intent chips should be shown in the typing panel.
    /// </summary>
    public bool ShowIntentChips => ShowIntentChipsToggle.IsOn;

    /// <summary>
    /// Gets whether practice suggestions should be shown in the stats panel.
    /// The user can hide suggestions entirely — no penalty.
    /// </summary>
    public bool ShowSuggestions => ShowSuggestionsToggle.IsOn;

    /// <summary>
    /// Whether community signal hints are shown.
    /// </summary>
    public bool ShowCommunitySignals => CommunitySignalsToggle.IsOn;

    /// <summary>
    /// Fired when the user toggles community signals on or off.
    /// </summary>
    public event EventHandler<bool>? CommunitySignalsChanged;

    /// <summary>
    /// Gets the user's default declared intent, or null for none.
    /// </summary>
    public UserIntent? DefaultIntent
    {
        get
        {
            var item = DefaultIntentCombo.SelectedItem as ComboBoxItem;
            var tag = item?.Tag?.ToString();
            return tag switch
            {
                "Focus" => UserIntent.Focus,
                "Challenge" => UserIntent.Challenge,
                "Maintenance" => UserIntent.Maintenance,
                "Exploration" => UserIntent.Exploration,
                _ => null
            };
        }
    }

    /// <summary>
    /// Gets the practice note text.
    /// </summary>
    public string? PracticeNote => string.IsNullOrWhiteSpace(PracticeNoteBox.Text)
        ? null
        : PracticeNoteBox.Text.Trim();

    /// <summary>
    /// Gets the active focus area, or null for none.
    /// </summary>
    public string? FocusArea
    {
        get
        {
            var item = FocusAreaCombo.SelectedItem as ComboBoxItem;
            var tag = item?.Tag?.ToString();
            return string.IsNullOrEmpty(tag) ? null : tag;
        }
    }

    /// <summary>
    /// Loads practice preferences from saved settings.
    /// </summary>
    public void LoadPracticePreferences(AppSettings settings)
    {
        ShowIntentChipsToggle.IsOn = settings.ShowIntentChips;
        ShowSuggestionsToggle.IsOn = settings.ShowSuggestions;
        CommunitySignalsToggle.IsOn = settings.ShowCommunitySignals;

        // Map DefaultIntent to combo index (0=None, 1=Focus, 2=Challenge, 3=Maintenance, 4=Exploration)
        DefaultIntentCombo.SelectedIndex = settings.DefaultIntent switch
        {
            UserIntent.Focus => 1,
            UserIntent.Challenge => 2,
            UserIntent.Maintenance => 3,
            UserIntent.Exploration => 4,
            _ => 0
        };

        PracticeNoteBox.Text = settings.PracticeNote ?? "";

        // Restore focus area — find by Tag match
        FocusAreaCombo.SelectedIndex = 0; // Default to "None"
        if (!string.IsNullOrEmpty(settings.FocusArea))
        {
            for (int i = 0; i < FocusAreaCombo.Items.Count; i++)
            {
                if (FocusAreaCombo.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), settings.FocusArea, StringComparison.OrdinalIgnoreCase))
                {
                    FocusAreaCombo.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    #endregion

    #region User Snippets

    /// <summary>
    /// Event fired when the user clicks "Open Snippets Folder".
    /// MainWindow handles the actual folder open.
    /// </summary>
    public event EventHandler? OpenUserSnippetsFolderRequested;

    /// <summary>
    /// Updates the user snippet status display.
    /// Call after SnippetService initialization.
    /// Status text is announced politely by screen readers.
    /// Errors are announced assertively but never block interaction.
    /// </summary>
    public void UpdateUserSnippetStatus(Services.UserContentService userContent)
    {
        if (userContent.HasUserContent)
        {
            var statusText = $"{userContent.TotalSnippetCount} snippets from {userContent.LoadedFileCount} file(s)";
            UserSnippetStatus.Text = statusText;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                UserSnippetStatus, $"Your snippets: {statusText}");
        }
        else if (userContent.UserSnippetsPath != null)
        {
            UserSnippetStatus.Text = "Snippets folder exists but is empty";
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                UserSnippetStatus, "Your snippets folder exists but contains no snippet files yet");
        }
        else
        {
            UserSnippetStatus.Text = "No snippets yet — open the folder below to get started";
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                UserSnippetStatus, "No user snippets loaded. Use the button below to open the snippets folder.");
        }

        // Show errors if any — accessible but non-blocking
        if (userContent.LoadErrors.Count > 0)
        {
            var errorText = string.Join("\n", userContent.LoadErrors.Take(3));
            UserSnippetErrors.Text = errorText;
            UserSnippetErrors.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                UserSnippetErrors, $"Snippet loading issues: {errorText}");
        }
        else
        {
            UserSnippetErrors.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        // Update button tooltip with actual path
        if (userContent.UserSnippetsPath != null)
        {
            ToolTipService.SetToolTip(OpenUserSnippetsButton,
                $"Open {userContent.UserSnippetsPath} in file explorer");
        }

        // Wire the button
        OpenUserSnippetsButton.Click -= OnOpenUserSnippetsClick;
        OpenUserSnippetsButton.Click += OnOpenUserSnippetsClick;
    }

    private void OnOpenUserSnippetsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OpenUserSnippetsFolderRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event fired when the user clicks "Export".
    /// </summary>
    public event EventHandler? ExportBundleRequested;

    /// <summary>
    /// Event fired when the user clicks "Import".
    /// </summary>
    public event EventHandler? ImportBundleRequested;

    /// <summary>
    /// Wires the export and import buttons.
    /// Called once during initialization.
    /// </summary>
    public void WireBundleButtons()
    {
        ExportBundleButton.Click += (_, _) => ExportBundleRequested?.Invoke(this, EventArgs.Empty);
        ImportBundleButton.Click += (_, _) => ImportBundleRequested?.Invoke(this, EventArgs.Empty);
        OpenCommunityFolderButton.Click += (_, _) => OpenCommunityFolderRequested?.Invoke(this, EventArgs.Empty);
        ImportCommunityBundleButton.Click += (_, _) => ImportBundleRequested?.Invoke(this, EventArgs.Empty);
        CommunitySignalsToggle.Toggled += (_, _) => CommunitySignalsChanged?.Invoke(this, CommunitySignalsToggle.IsOn);
    }

    /// <summary>
    /// Shows a status message after an export or import operation.
    /// </summary>
    public void ShowBundleStatus(string message)
    {
        BundleStatus.Text = message;
        BundleStatus.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            BundleStatus, message);
    }

    #endregion

    #region Community Content

    /// <summary>
    /// Event fired when the user clicks "Open Community Folder".
    /// </summary>
    public event EventHandler? OpenCommunityFolderRequested;

    /// <summary>
    /// Updates the community content status display.
    /// Follows the same pattern as UpdateUserSnippetStatus.
    /// </summary>
    public void UpdateCommunityContentStatus(Services.CommunityContentService communityContent)
    {
        if (CommunityContentStatus == null) return;

        if (communityContent.HasCommunityContent)
        {
            // Use the enhanced summary that includes age info
            var statusText = communityContent.GetContentSummary();
            CommunityContentStatus.Text = statusText;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                CommunityContentStatus, $"Community content: {statusText}");
        }
        else if (communityContent.CommunityContentPath != null)
        {
            CommunityContentStatus.Text = "Community folder exists but is empty";
        }
        else
        {
            CommunityContentStatus.Text = "No community content";
        }

        // Show load errors if any
        if (communityContent.LoadErrors.Count > 0)
        {
            var errorText = string.Join("\n", communityContent.LoadErrors.Take(3));
            CommunityContentStatus.Text += $"\n{errorText}";
        }
    }

    #endregion
}
