using Microsoft.UI.Xaml.Controls;

namespace DevOpTyper.Panels;

public sealed partial class StatsPanel : UserControl
{
    public StatsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates all stats display.
    /// </summary>
    public void UpdateStats(double wpm, double accuracy, int errors, int typedLength, int targetLength, int xp)
    {
        WpmText.Text = wpm.ToString("F0");
        AccuracyText.Text = $"{accuracy:F1}%";
        ErrorsText.Text = errors.ToString();
        
        double progress = targetLength > 0 ? 100.0 * typedLength / targetLength : 0;
        ProgressBar.Value = Math.Min(100, progress);
        ProgressText.Text = $"{typedLength} / {targetLength} chars";
        
        XpText.Text = $"+{xp} XP";
    }

    /// <summary>
    /// Resets stats to initial values.
    /// </summary>
    public void Reset()
    {
        WpmText.Text = "0";
        AccuracyText.Text = "100%";
        ErrorsText.Text = "0";
        ProgressBar.Value = 0;
        ProgressText.Text = "0 / 0 chars";
        XpText.Text = "+0 XP";
    }
}
