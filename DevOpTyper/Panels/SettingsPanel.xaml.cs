using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace DevOpTyper.Panels;

public sealed partial class SettingsPanel : UserControl
{
    // Volume change events so MainWindow can update AudioService
    public event EventHandler<double>? AmbientVolumeChanged;
    public event EventHandler<double>? KeyboardVolumeChanged;
    public event EventHandler<double>? UiVolumeChanged;

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
}
