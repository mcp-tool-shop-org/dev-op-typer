using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

/// <summary>
/// Displays explanatory perspectives for the current snippet.
///
/// Perspectives are optional, collapsible, and never interrupt typing.
/// They are visible only between sessions — hidden during active practice.
/// Multiple perspectives coexist without hierarchy.
/// </summary>
public sealed partial class ExplanationPanel : UserControl
{
    public ExplanationPanel()
    {
        InitializeComponent();

        ExpandToggle.Checked += (_, _) => ContentArea.Visibility = Visibility.Visible;
        ExpandToggle.Unchecked += (_, _) => ContentArea.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Sets the snippet whose perspectives should be displayed.
    /// If the snippet has no explanations or perspectives, the panel stays hidden.
    /// </summary>
    public void SetSnippet(Snippet? snippet)
    {
        if (snippet == null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        bool hasExplain = snippet.Explain != null && snippet.Explain.Length > 0;
        bool hasPerspectives = snippet.Perspectives != null && snippet.Perspectives.Length > 0;

        if (!hasExplain && !hasPerspectives)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        // Show panel (collapsed content by default — user clicks to expand)
        Visibility = Visibility.Visible;
        ExpandToggle.IsChecked = false;
        ContentArea.Visibility = Visibility.Collapsed;

        // Legacy explain field
        if (hasExplain)
        {
            LegacyExplainSection.Visibility = Visibility.Visible;
            LegacyExplainList.ItemsSource = snippet.Explain;
        }
        else
        {
            LegacyExplainSection.Visibility = Visibility.Collapsed;
            LegacyExplainList.ItemsSource = null;
        }

        // Perspectives
        if (hasPerspectives)
        {
            var perspectiveElements = new List<StackPanel>();

            // Cap at 5 to prevent UI clutter
            foreach (var perspective in snippet.Perspectives!.Take(ExtensionBoundary.MaxPerspectivesPerSnippet))
            {
                if (string.IsNullOrWhiteSpace(perspective.Label) || perspective.Notes == null || perspective.Notes.Length == 0)
                    continue;

                var section = new StackPanel { Spacing = 2 };

                // Perspective label
                section.Children.Add(new TextBlock
                {
                    Text = perspective.Label,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Resources["TextFillColorSecondaryBrush"]
                });

                // Notes
                foreach (var note in perspective.Notes.Take(ExtensionBoundary.MaxNotesPerPerspective))
                {
                    section.Children.Add(new TextBlock
                    {
                        Text = note,
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                        Margin = new Thickness(8, 0, 0, 2),
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Resources["TextFillColorTertiaryBrush"]
                    });
                }

                perspectiveElements.Add(section);
            }

            PerspectivesList.ItemsSource = perspectiveElements;
        }
        else
        {
            PerspectivesList.ItemsSource = null;
        }
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
        // Only show if there's actually content to display
        if (PerspectivesList.ItemsSource != null || LegacyExplainSection.Visibility == Visibility.Visible)
        {
            Visibility = Visibility.Visible;
        }
    }
}
