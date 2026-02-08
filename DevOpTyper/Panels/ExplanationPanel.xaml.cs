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
///
/// When a snippet has both a legacy "explain" field and new "perspectives",
/// the legacy content is merged as a "Notes" perspective so all explanatory
/// material appears in one unified list.
/// </summary>
public sealed partial class ExplanationPanel : UserControl
{
    private bool _hasContent;

    public ExplanationPanel()
    {
        InitializeComponent();

        ExpandToggle.Checked += (_, _) => ContentArea.Visibility = Visibility.Visible;
        ExpandToggle.Unchecked += (_, _) => ContentArea.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Sets the snippet whose perspectives should be displayed.
    /// If the snippet has no explanations or perspectives, the panel stays hidden.
    ///
    /// Legacy "explain" arrays are merged as a "Notes" perspective when
    /// both fields are present. When only "explain" exists, it becomes
    /// the sole perspective. This ensures all explanatory material appears
    /// in one consistent format.
    /// </summary>
    public void SetSnippet(Snippet? snippet)
    {
        _hasContent = false;

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

        // Build unified perspective list
        var allPerspectives = new List<ExplanationSet>();

        // Merge legacy explain as "Notes" perspective
        if (hasExplain)
        {
            allPerspectives.Add(new ExplanationSet
            {
                Label = "Notes",
                Notes = snippet.Explain!
            });
        }

        // Add explicit perspectives
        if (hasPerspectives)
        {
            foreach (var p in snippet.Perspectives!.Take(ExtensionBoundary.MaxPerspectivesPerSnippet))
            {
                if (!string.IsNullOrWhiteSpace(p.Label) && p.Notes != null && p.Notes.Length > 0)
                {
                    allPerspectives.Add(p);
                }
            }
        }

        if (allPerspectives.Count == 0)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        // Show panel (collapsed content by default — user clicks to expand)
        Visibility = Visibility.Visible;
        _hasContent = true;
        ExpandToggle.IsChecked = false;
        ContentArea.Visibility = Visibility.Collapsed;

        // Build perspective UI elements
        var perspectiveElements = new List<StackPanel>();

        foreach (var perspective in allPerspectives)
        {
            var section = new StackPanel { Spacing = 2 };

            // Perspective label as heading
            section.Children.Add(new TextBlock
            {
                Text = perspective.Label,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            // Notes (capped per perspective)
            foreach (var note in perspective.Notes.Take(ExtensionBoundary.MaxNotesPerPerspective))
            {
                section.Children.Add(new TextBlock
                {
                    Text = note,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    Margin = new Thickness(8, 0, 0, 2),
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
            }

            perspectiveElements.Add(section);
        }

        PerspectivesList.ItemsSource = perspectiveElements;
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
