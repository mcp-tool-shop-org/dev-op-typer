using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevOpTyper.Models;
using DevOpTyper.Services;

namespace DevOpTyper.Panels;

public sealed partial class StatsPanel : UserControl
{
    /// <summary>
    /// Fired when the user clicks "Try" on a suggestion.
    /// The payload is the PracticeSuggestion they chose.
    /// </summary>
    public event EventHandler<PracticeSuggestion>? SuggestionFollowed;

    /// <summary>
    /// Fired when the user clicks "Practice" on a weak character.
    /// The payload is a set of weak characters to target.
    /// </summary>
    public event EventHandler<HashSet<char>>? WeaknessPracticeRequested;

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
    /// Updates the Weak Spots section from the heatmap, with optional trajectory context.
    /// </summary>
    public void UpdateWeakSpots(MistakeHeatmap heatmap, WeaknessReport? report = null)
    {
        WeakSpotsContainer.Children.Clear();

        // Show weakness summary if report has trajectory data (Phase 3)
        if (report != null && report.HasData && !string.IsNullOrEmpty(report.Summary))
        {
            WeaknessSummaryText.Text = report.Summary;
            WeaknessSummaryText.Visibility = Visibility.Visible;
        }
        else
        {
            WeaknessSummaryText.Visibility = Visibility.Collapsed;
        }

        var weakest = heatmap.GetWeakest(count: 5, minAttempts: 3);

        if (weakest.Count == 0)
        {
            WeakSpotsEmpty.Visibility = Visibility.Visible;
            WeakSpotsContainer.Visibility = Visibility.Collapsed;
            return;
        }

        WeakSpotsEmpty.Visibility = Visibility.Collapsed;
        WeakSpotsContainer.Visibility = Visibility.Visible;

        // Build a trajectory lookup from the report
        var trajectoryMap = new Dictionary<char, WeaknessTrajectory>();
        if (report?.Items != null)
        {
            foreach (var item in report.Items)
                trajectoryMap[item.Character] = item.Trajectory;
        }

        foreach (var w in weakest)
        {
            trajectoryMap.TryGetValue(w.Character, out var trajectory);
            var row = CreateWeakSpotRow(w, trajectory);
            WeakSpotsContainer.Children.Add(row);
        }

        // Show resolved weaknesses if any (Phase 3)
        if (report?.ResolvedWeaknesses.Count > 0)
        {
            var resolvedHeader = new TextBlock
            {
                Text = "Resolved",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 80, 200, 120)),
                Margin = new Thickness(0, 4, 0, 0)
            };
            WeakSpotsContainer.Children.Add(resolvedHeader);

            foreach (var resolved in report.ResolvedWeaknesses.Take(3))
            {
                string charDisp = resolved.Character == ' ' ? "SP" : resolved.Character.ToString();
                var resolvedRow = new TextBlock
                {
                    Text = $"\u2713 {charDisp} ({resolved.OldErrorRate:P0} \u2192 {resolved.CurrentErrorRate:P0})",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 80, 200, 120))
                };
                WeakSpotsContainer.Children.Add(resolvedRow);
            }
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

        // "Practice These" button — loads a snippet targeting these weak chars
        var weakChars = new HashSet<char>(weakest.Select(w => w.Character));
        var practiceButton = new Button
        {
            Content = "Practice These",
            FontSize = 10,
            Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        practiceButton.Click += (_, _) => WeaknessPracticeRequested?.Invoke(this, weakChars);
        WeakSpotsContainer.Children.Add(practiceButton);
    }

    /// <summary>
    /// Updates the pacing indicator from SessionPacer snapshot.
    /// </summary>
    public void UpdatePacing(PacingSnapshot pacing)
    {
        if (pacing.SessionsThisLaunch == 0 && pacing.SessionsToday == 0)
        {
            PacingLabel.Visibility = Visibility.Collapsed;
            return;
        }

        PacingLabel.Visibility = Visibility.Visible;
        PacingLabel.Text = $"{pacing.PaceLabel} \u2022 {pacing.SessionsToday} today \u2022 {pacing.TimeSinceLastSession}";
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

    /// <summary>
    /// Updates the Trends section from trend analysis.
    /// </summary>
    public void UpdateTrends(List<LanguageTrendSummary> trends)
    {
        TrendContainer.Children.Clear();

        if (trends.Count == 0)
        {
            TrendDivider.Visibility = Visibility.Collapsed;
            TrendHeader.Visibility = Visibility.Collapsed;
            TrendContainer.Visibility = Visibility.Collapsed;
            return;
        }

        TrendDivider.Visibility = Visibility.Visible;
        TrendHeader.Visibility = Visibility.Visible;
        TrendContainer.Visibility = Visibility.Visible;

        foreach (var trend in trends.Take(3))
        {
            var row = CreateTrendRow(trend);
            TrendContainer.Children.Add(row);
        }
    }

    /// <summary>
    /// Updates the Suggestions section from practice recommendations.
    /// </summary>
    public void UpdateSuggestions(List<PracticeSuggestion> suggestions)
    {
        SuggestionContainer.Children.Clear();

        if (suggestions.Count == 0)
        {
            SuggestionDivider.Visibility = Visibility.Collapsed;
            SuggestionHeader.Visibility = Visibility.Collapsed;
            SuggestionContainer.Visibility = Visibility.Collapsed;
            return;
        }

        SuggestionDivider.Visibility = Visibility.Visible;
        SuggestionHeader.Visibility = Visibility.Visible;
        SuggestionContainer.Visibility = Visibility.Visible;

        foreach (var suggestion in suggestions.Take(3))
        {
            var row = CreateSuggestionRow(suggestion);
            SuggestionContainer.Children.Add(row);
        }
    }

    #region UI Row Builders

    private static Grid CreateWeakSpotRow(CharWeakness w, WeaknessTrajectory trajectory = WeaknessTrajectory.New)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Character display with trajectory indicator
        string charDisplay = w.Character switch
        {
            ' ' => "SP",
            '\t' => "TAB",
            '\n' => "LF",
            '\r' => "CR",
            _ => w.Character.ToString()
        };

        // Trajectory arrow prefix
        string trajectoryMark = trajectory switch
        {
            WeaknessTrajectory.Improving => "\u2193", // Down arrow (error rate decreasing)
            WeaknessTrajectory.Worsening => "\u2191", // Up arrow (error rate increasing)
            WeaknessTrajectory.Steady => "\u2022",    // Bullet (stable)
            _ => ""                                    // New — no mark
        };

        var charBlock = new TextBlock
        {
            Text = trajectoryMark.Length > 0 ? $"{trajectoryMark}{charDisplay}" : charDisplay,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = trajectory switch
            {
                WeaknessTrajectory.Improving => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 80, 200, 120)),
                WeaknessTrajectory.Worsening => (Brush)Application.Current.Resources["DotErrorBrush"],
                _ => (Brush)Application.Current.Resources["DotTextBrush"]
            }
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

        // Show note if present (v0.4.0)
        if (!string.IsNullOrEmpty(session.Note))
        {
            var noteBlock = new TextBlock
            {
                Text = $"\u270E {session.Note}",
                FontSize = 10,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = (Brush)Application.Current.Resources["DotTextMutedBrush"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1
            };
            stack.Children.Add(noteBlock);
        }

        border.Child = stack;
        return border;
    }

    private static Border CreateTrendRow(LanguageTrendSummary trend)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["DotSurface2Brush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6)
        };

        var stack = new StackPanel { Spacing = 2 };

        // Language + direction indicator
        string arrow = trend.OverallMomentum switch
        {
            Momentum.StrongPositive => "\u2191\u2191",
            Momentum.Positive => "\u2191",
            Momentum.Neutral => "\u2194",
            Momentum.Negative => "\u2193",
            Momentum.StrongNegative => "\u2193\u2193",
            _ => ""
        };

        var titleBlock = new TextBlock
        {
            Text = $"{trend.Language} {arrow}",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        // Stats line
        string wpmDir = trend.WpmDirection switch
        {
            TrendDirection.Improving => "+",
            TrendDirection.Declining => "-",
            _ => "="
        };
        string accDir = trend.AccuracyDirection switch
        {
            TrendDirection.Improving => "+",
            TrendDirection.Declining => "-",
            _ => "="
        };

        var statsBlock = new TextBlock
        {
            Text = $"WPM {trend.RecentAvgWpm:F0} ({wpmDir}) | Acc {trend.RecentAvgAccuracy:F0}% ({accDir}) | {trend.SessionCount} sessions",
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["DotTextMutedBrush"],
            TextWrapping = TextWrapping.Wrap
        };

        stack.Children.Add(titleBlock);
        stack.Children.Add(statsBlock);

        border.Child = stack;
        return border;
    }

    private Border CreateSuggestionRow(PracticeSuggestion suggestion)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6),
            Opacity = suggestion.Priority == SuggestionPriority.Low ? 0.7 : 1.0
        };

        // Color based on suggestion type
        border.Background = suggestion.Type switch
        {
            SuggestionType.TakeBreak => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 200, 50)),
            SuggestionType.TargetWeakness => (Brush)Application.Current.Resources["DotSurface2Brush"],
            _ => (Brush)Application.Current.Resources["DotSurface2Brush"]
        };

        var stack = new StackPanel { Spacing = 2 };

        // Title row with optional action button
        if (suggestion.Action != SuggestionAction.None)
        {
            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = suggestion.Title,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);

            var tryButton = new Button
            {
                Content = "Try",
                FontSize = 10,
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            tryButton.Click += (_, _) => SuggestionFollowed?.Invoke(this, suggestion);
            Grid.SetColumn(tryButton, 1);

            titleGrid.Children.Add(titleBlock);
            titleGrid.Children.Add(tryButton);
            stack.Children.Add(titleGrid);
        }
        else
        {
            var titleBlock = new TextBlock
            {
                Text = suggestion.Title,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            stack.Children.Add(titleBlock);
        }

        var reasonBlock = new TextBlock
        {
            Text = suggestion.Reason,
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["DotTextMutedBrush"],
            TextWrapping = TextWrapping.Wrap
        };

        stack.Children.Add(reasonBlock);

        border.Child = stack;
        return border;
    }

    #endregion
}
