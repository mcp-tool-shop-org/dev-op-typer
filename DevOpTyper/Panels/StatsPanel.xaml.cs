using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

public sealed partial class StatsPanel : UserControl
{
    public StatsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates live stats display during a session.
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
    /// Resets live stats to initial values.
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

    /// <summary>
    /// Updates the Weak Spots section from the heatmap.
    /// Call after each session completes to show current weaknesses.
    /// </summary>
    public void UpdateWeakSpots(MistakeHeatmap heatmap)
    {
        WeakSpotsContainer.Children.Clear();

        var weakest = heatmap.GetWeakest(count: 5, minAttempts: 3);

        if (weakest.Count == 0)
        {
            WeakSpotsEmpty.Visibility = Visibility.Visible;
            WeakSpotsContainer.Visibility = Visibility.Collapsed;
            return;
        }

        WeakSpotsEmpty.Visibility = Visibility.Collapsed;
        WeakSpotsContainer.Visibility = Visibility.Visible;

        foreach (var w in weakest)
        {
            var row = CreateWeakSpotRow(w);
            WeakSpotsContainer.Children.Add(row);
        }

        // Also show group weaknesses if any
        var weakGroups = heatmap.GetWeakestGroups(minAttempts: 5);
        if (weakGroups.Count > 0)
        {
            var groupHeader = new TextBlock
            {
                Text = "By Group",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)App.Current.Resources["DotTextMutedBrush"],
                Margin = new Thickness(0, 4, 0, 0)
            };
            WeakSpotsContainer.Children.Add(groupHeader);

            foreach (var g in weakGroups.Take(3))
            {
                var groupRow = CreateGroupWeaknessRow(g);
                WeakSpotsContainer.Children.Add(groupRow);
            }
        }
    }

    /// <summary>
    /// Updates the Recent Sessions section from history.
    /// Call after each session completes to show latest results.
    /// </summary>
    public void UpdateHistory(SessionHistory history)
    {
        HistoryContainer.Children.Clear();

        var recent = history.Records.Take(5).ToList();

        if (recent.Count == 0)
        {
            HistoryEmpty.Visibility = Visibility.Visible;
            HistoryContainer.Visibility = Visibility.Collapsed;
            LifetimeStatsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        HistoryEmpty.Visibility = Visibility.Collapsed;
        HistoryContainer.Visibility = Visibility.Visible;

        foreach (var session in recent)
        {
            var row = CreateSessionRow(session);
            HistoryContainer.Children.Add(row);
        }

        // Show lifetime stats if enough data
        if (history.TotalSessions >= 3)
        {
            var stats = history.GetLifetimeStats();
            var bests = history.GetPersonalBests();

            LifetimeStatsPanel.Visibility = Visibility.Visible;
            LifetimeText.Text = $"{stats.TotalSessions} sessions | " +
                                $"Avg {stats.AverageWpm:F0} WPM | " +
                                $"Avg {stats.AverageAccuracy:F0}%\n" +
                                $"Best: {bests.BestWpm:F0} WPM | " +
                                $"{stats.PerfectSessions} perfect";
        }
        else
        {
            LifetimeStatsPanel.Visibility = Visibility.Collapsed;
        }
    }

    #region UI Row Builders

    private static Grid CreateWeakSpotRow(CharWeakness w)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Character display
        string charDisplay = w.Character switch
        {
            ' ' => "SP",
            '\t' => "TAB",
            '\n' => "LF",
            '\r' => "CR",
            _ => w.Character.ToString()
        };

        var charBlock = new TextBlock
        {
            Text = charDisplay,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(charBlock, 0);

        // Error rate + confusion info
        string detail = $"{w.ErrorRate * 100:F0}% err ({w.TotalMisses}/{w.TotalAttempts})";
        if (w.TopConfusion != default)
        {
            detail += $" \u2192 '{w.TopConfusion}'";
        }

        var detailBlock = new TextBlock
        {
            Text = detail,
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["DotTextMutedBrush"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        Grid.SetColumn(detailBlock, 1);

        // Error rate bar (visual indicator)
        var barBorder = new Border
        {
            Width = 40,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Colors.DarkGray),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var fillWidth = Math.Min(40, w.ErrorRate * 40);
        var fillBar = new Border
        {
            Width = fillWidth,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Color: red for high error rate, yellow for moderate
        if (w.ErrorRate > 0.3)
            fillBar.Background = (Brush)Application.Current.Resources["DotErrorBrush"];
        else
            fillBar.Background = (Brush)Application.Current.Resources["DotAccentWarnBrush"];

        barBorder.Child = fillBar;
        Grid.SetColumn(barBorder, 2);

        grid.Children.Add(charBlock);
        grid.Children.Add(detailBlock);
        grid.Children.Add(barBorder);

        return grid;
    }

    private static Grid CreateGroupWeaknessRow(GroupWeakness g)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text = g.Group.ToString(),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameBlock, 0);

        var rateBlock = new TextBlock
        {
            Text = $"{g.ErrorRate * 100:F0}% ({g.TotalMisses} miss)",
            FontSize = 11,
            Foreground = g.ErrorRate > 0.2
                ? (Brush)Application.Current.Resources["DotErrorBrush"]
                : (Brush)Application.Current.Resources["DotAccentWarnBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(rateBlock, 1);

        grid.Children.Add(nameBlock);
        grid.Children.Add(rateBlock);

        return grid;
    }

    private static Border CreateSessionRow(SessionRecord session)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["DotSurface2Brush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6)
        };

        var stack = new StackPanel { Spacing = 2 };

        // Title row
        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = session.SnippetTitle,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 140
        };
        Grid.SetColumn(titleBlock, 0);

        var xpBlock = new TextBlock
        {
            Text = $"+{session.XpEarned}",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["DotAccentWarnBrush"]
        };
        Grid.SetColumn(xpBlock, 1);

        titleGrid.Children.Add(titleBlock);
        titleGrid.Children.Add(xpBlock);

        // Stats row
        string statsLine = $"{session.Wpm:F0} WPM | {session.Accuracy:F0}% | {session.ErrorCount} err";
        if (session.IsPerfect) statsLine += " \u2605"; // Star for perfect

        var statsBlock = new TextBlock
        {
            Text = statsLine,
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["DotTextMutedBrush"]
        };

        stack.Children.Add(titleGrid);
        stack.Children.Add(statsBlock);

        border.Child = stack;
        return border;
    }

    #endregion
}
