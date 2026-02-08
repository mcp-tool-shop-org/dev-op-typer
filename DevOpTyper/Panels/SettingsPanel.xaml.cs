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

    // Guards to prevent false events during programmatic population
    private bool _suppressThemeEvent;
    private bool _suppressSoundscapeEvent;

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

        // Accuracy floor slider label update
        AccuracyFloorSlider.ValueChanged += (s, e) =>
        {
            AccuracyFloorLabel.Text = $"{(int)e.NewValue}%";
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
    /// Loads practice preferences from saved settings.
    /// </summary>
    public void LoadPracticePreferences(AppSettings settings)
    {
        ShowIntentChipsToggle.IsOn = settings.ShowIntentChips;

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
    }

    #endregion
}
