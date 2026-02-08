using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

/// <summary>
/// Displays alternative approaches for the current snippet.
///
/// Demonstrations show different valid ways to solve the same problem.
/// They are visible only between sessions — hidden during active typing.
/// No demonstration is ranked, recommended, or attributed to an author.
/// All approaches have equal visual weight.
/// </summary>
public sealed partial class DemonstrationPanel : UserControl
{
    private bool _hasContent;

    public DemonstrationPanel()
    {
        InitializeComponent();

        ExpandToggle.Checked += (_, _) => ContentArea.Visibility = Visibility.Visible;
        ExpandToggle.Unchecked += (_, _) => ContentArea.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Sets the snippet whose demonstrations should be displayed.
    /// If the snippet has no demonstrations, the panel stays hidden.
    /// </summary>
    public void SetSnippet(Snippet? snippet)
    {
        _hasContent = false;

        if (snippet == null || snippet.Demonstrations == null || snippet.Demonstrations.Length == 0)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        var demos = snippet.Demonstrations
            .Where(d => !string.IsNullOrWhiteSpace(d.Label) && !string.IsNullOrWhiteSpace(d.Code))
            .Take(ExtensionBoundary.MaxDemonstrationsPerSnippet)
            .ToList();

        if (demos.Count == 0)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        // Show panel (collapsed content by default — user clicks to expand)
        Visibility = Visibility.Visible;
        _hasContent = true;
        ExpandToggle.IsChecked = false;
        ContentArea.Visibility = Visibility.Collapsed;

        // Build demonstration UI elements
        var elements = new List<Border>();

        foreach (var demo in demos)
        {
            var card = new StackPanel { Spacing = 4 };

            // Label as accessible heading
            var label = new TextBlock
            {
                Text = demo.Label,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHeadingLevel(
                label, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level3);
            card.Children.Add(label);

            // Code in monospace (display-only, selectable)
            var code = demo.Code;
            if (code.Length > ExtensionBoundary.MaxDemonstrationCodeLength)
                code = code[..ExtensionBoundary.MaxDemonstrationCodeLength] + "…";

            card.Children.Add(new TextBlock
            {
                Text = code,
                FontSize = 11,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                IsTabStop = false,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            });

            // Optional description
            if (!string.IsNullOrWhiteSpace(demo.Description))
            {
                var desc = demo.Description;
                if (desc.Length > ExtensionBoundary.MaxDemonstrationDescriptionLength)
                    desc = desc[..ExtensionBoundary.MaxDemonstrationDescriptionLength] + "…";

                card.Children.Add(new TextBlock
                {
                    Text = desc,
                    FontSize = 10,
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
            }

            // Wrap in a bordered card
            var border = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Child = card
            };

            elements.Add(border);
        }

        DemonstrationsList.ItemsSource = elements;
    }

    /// <summary>
    /// Hides the panel. Called when a typing session starts.
    /// </summary>
    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Shows the panel if it has content. Called between sessions.
    /// </summary>
    public void Show()
    {
        if (_hasContent)
        {
            Visibility = Visibility.Visible;
        }
    }
}
