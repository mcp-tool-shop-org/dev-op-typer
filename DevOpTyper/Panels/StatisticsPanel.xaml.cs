using Microsoft.UI.Xaml.Controls;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

/// <summary>
/// Panel for displaying user statistics and progress.
/// </summary>
public sealed partial class StatisticsPanel : UserControl
{
    public StatisticsPanel()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Updates all statistics displays from the persisted data.
    /// </summary>
    public void UpdateStats(PersistedBlob data)
    {
        if (data == null) return;

        UpdatePersonalBests(data.History.GetPersonalBests());
        UpdateTodayStats(data.History.GetTodayStats());
        UpdateLifetimeStats(data.History.GetLifetimeStats());
        UpdateProfile(data.Profile);
    }

    private void UpdatePersonalBests(PersonalBests bests)
    {
        BestWpmText.Text = FormatNumber(bests.BestWpm);
        BestAccuracyText.Text = $"{bests.BestAccuracy:F1}%";
        BestStreakText.Text = bests.LongestStreak.ToString();
        FastestPerfectText.Text = bests.FastestPerfect > 0 ? $"{FormatNumber(bests.FastestPerfect)} WPM" : "—";
    }

    private void UpdateTodayStats(HistoryStatistics stats)
    {
        TodaySessionsText.Text = stats.TotalSessions.ToString();
        TodayWpmText.Text = stats.TotalSessions > 0 ? FormatNumber(stats.AverageWpm) : "—";
        TodayAccuracyText.Text = stats.TotalSessions > 0 ? $"{stats.AverageAccuracy:F1}%" : "—";
    }

    private void UpdateLifetimeStats(HistoryStatistics stats)
    {
        TotalSessionsText.Text = FormatLargeNumber(stats.TotalSessions);
        TotalCharsText.Text = FormatLargeNumber(stats.TotalCharacters);
        TotalTimeText.Text = FormatDuration(stats.TotalDurationMinutes);
        AvgWpmText.Text = stats.TotalSessions > 0 ? FormatNumber(stats.AverageWpm) : "—";
        AvgAccuracyText.Text = stats.TotalSessions > 0 ? $"{stats.AverageAccuracy:F1}%" : "—";
        PerfectSessionsText.Text = $"{stats.PerfectSessions} ({stats.PerfectRate:F0}%)";
        HardcoreSessionsText.Text = stats.HardcoreSessions.ToString();
    }

    private void UpdateProfile(Profile profile)
    {
        LevelText.Text = profile.Level.ToString();
        
        var xpNeeded = Profile.XpNeededForNext(profile.Level);
        XpText.Text = $"{profile.Xp} / {xpNeeded}";
        XpProgressBar.Maximum = xpNeeded;
        XpProgressBar.Value = profile.Xp;
    }

    private static string FormatNumber(double value)
    {
        return value >= 100 ? $"{value:F0}" : $"{value:F1}";
    }

    private static string FormatLargeNumber(int value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000.0:F1}M";
        if (value >= 1_000) return $"{value / 1_000.0:F1}K";
        return value.ToString();
    }

    private static string FormatDuration(double minutes)
    {
        if (minutes >= 60)
        {
            var hours = (int)(minutes / 60);
            var mins = (int)(minutes % 60);
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
        return $"{(int)minutes}m";
    }
}
