using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

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
    /// Gets whether hardcore mode is enabled.
    /// </summary>
    public bool IsHardcoreMode => HardcoreModeToggle.IsOn;

    /// <summary>
    /// Gets whether high contrast mode is enabled.
    /// </summary>
    public bool IsHighContrast => HighContrastToggle.IsOn;

    /// <summary>
    /// Gets whether reduced motion is enabled.
    /// </summary>
    public bool IsReducedMotion => ReducedMotionToggle.IsOn;

    /// <summary>
    /// Gets or sets the selected keyboard theme (now string items, not ComboBoxItem).
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
}
