using Microsoft.UI.Xaml;

namespace DevOpTyper.Services;

/// <summary>
/// Manages theme switching including high contrast support.
/// </summary>
public sealed class ThemeManager
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    private AppTheme _currentTheme = AppTheme.Default;
    private bool _isHighContrastActive;
    private readonly AccessibilitySettings _accessibilitySettings;

    // Events
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    private ThemeManager()
    {
        _accessibilitySettings = AccessibilitySettings.Instance;
        _accessibilitySettings.SettingsChanged += OnAccessibilitySettingsChanged;

        // Initialize based on system settings
        DetectTheme();
    }

    /// <summary>
    /// Current application theme.
    /// </summary>
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set => SetTheme(value);
    }

    /// <summary>
    /// Whether high contrast mode is currently active.
    /// </summary>
    public bool IsHighContrastActive => _isHighContrastActive;

    /// <summary>
    /// Set the application theme.
    /// </summary>
    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme == theme) return;

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        ApplyTheme();
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
    }

    /// <summary>
    /// Toggle between default and high contrast themes.
    /// </summary>
    public void ToggleHighContrast()
    {
        if (_currentTheme == AppTheme.HighContrast)
        {
            SetTheme(AppTheme.Default);
        }
        else
        {
            SetTheme(AppTheme.HighContrast);
        }
    }

    /// <summary>
    /// Detect and apply system theme preferences.
    /// </summary>
    public void DetectTheme()
    {
        _isHighContrastActive = _accessibilitySettings.HighContrast;

        if (_isHighContrastActive)
        {
            _currentTheme = AppTheme.HighContrast;
        }

        ApplyTheme();
    }

    /// <summary>
    /// Apply current theme to the application.
    /// </summary>
    private void ApplyTheme()
    {
        try
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Get merged dictionaries
            var merged = app.Resources.MergedDictionaries;
            if (merged.Count == 0) return;

            // Find and remove existing theme dictionary
            ResourceDictionary? existingTheme = null;
            foreach (var dict in merged)
            {
                if (dict.Source?.OriginalString?.Contains("Themes/") == true)
                {
                    existingTheme = dict;
                    break;
                }
            }

            if (existingTheme != null)
            {
                merged.Remove(existingTheme);
            }

            // Add new theme dictionary
            var themeUri = _currentTheme switch
            {
                AppTheme.HighContrast => new Uri("ms-appx:///Themes/HighContrast.xaml"),
                _ => new Uri("ms-appx:///Themes/Colors.xaml")
            };

            var newTheme = new ResourceDictionary { Source = themeUri };
            merged.Add(newTheme);

            _isHighContrastActive = _currentTheme == AppTheme.HighContrast;
        }
        catch
        {
            // Silently fail if theme switching fails
        }
    }

    private void OnAccessibilitySettingsChanged(object? sender, AccessibilityChangedEventArgs e)
    {
        if (e.SettingName is nameof(AccessibilitySettings.HighContrast) or "*")
        {
            if (_accessibilitySettings.HighContrast && _currentTheme != AppTheme.HighContrast)
            {
                SetTheme(AppTheme.HighContrast);
            }
            else if (!_accessibilitySettings.HighContrast && _currentTheme == AppTheme.HighContrast)
            {
                SetTheme(AppTheme.Default);
            }
        }
    }

    #region Color Helpers

    /// <summary>
    /// Get a brush resource from the current theme.
    /// </summary>
    public Microsoft.UI.Xaml.Media.SolidColorBrush? GetBrush(string resourceKey)
    {
        try
        {
            if (Application.Current?.Resources?.TryGetValue(resourceKey, out var value) == true)
            {
                return value as Microsoft.UI.Xaml.Media.SolidColorBrush;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Get a color resource from the current theme.
    /// </summary>
    public Windows.UI.Color? GetColor(string resourceKey)
    {
        try
        {
            if (Application.Current?.Resources?.TryGetValue(resourceKey, out var value) == true)
            {
                return value as Windows.UI.Color?;
            }
        }
        catch { }
        return null;
    }

    #endregion
}

/// <summary>
/// Application themes.
/// </summary>
public enum AppTheme
{
    /// <summary>Default dark theme.</summary>
    Default,

    /// <summary>High contrast theme for accessibility.</summary>
    HighContrast,

    /// <summary>Light theme (future).</summary>
    Light
}

/// <summary>
/// Event args for theme changes.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public AppTheme OldTheme { get; }
    public AppTheme NewTheme { get; }

    public ThemeChangedEventArgs(AppTheme oldTheme, AppTheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }
}
