using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

/// <summary>
/// Displays skill depth layers for the current snippet.
///
/// Layers offer different depths on the same snippet — "Essentials",
/// "Deeper", "Advanced". Labels describe the content's depth, never
/// the user's level. All layers are accessible to all users at all times.
///
/// No layer is gated by user level, rating, or session count.
/// No layer is recommended or labeled as "start here".
/// The system never tracks which layers the user expands.
/// </summary>
public sealed partial class LayersPanel : UserControl
{
    private bool _hasContent;

    public LayersPanel()
    {
        InitializeComponent();

        ExpandToggle.Checked += (_, _) => ContentArea.Visibility = Visibility.Visible;
        ExpandToggle.Unchecked += (_, _) => ContentArea.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Sets the snippet whose layers should be displayed.
    /// If the snippet has no layers, the panel stays hidden.
    /// Each layer is individually expandable — user chooses depth.
    /// </summary>
    public void SetSnippet(Snippet? snippet)
    {
        _hasContent = false;

        if (snippet == null || snippet.Layers == null || snippet.Layers.Length == 0)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        var layers = snippet.Layers
            .Where(l => !string.IsNullOrWhiteSpace(l.Label) && l.Content.Length > 0)
            .Take(ExtensionBoundary.MaxLayersPerSnippet)
            .ToList();

        if (layers.Count == 0)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        // Show panel (collapsed content by default — user clicks to expand)
        Visibility = Visibility.Visible;
        _hasContent = true;
        ExpandToggle.IsChecked = false;
        ContentArea.Visibility = Visibility.Collapsed;

        // Build layer UI elements — each individually expandable.
        // All layers start collapsed. The user chooses which depth to explore.
        // No layer is pre-expanded, recommended, or labeled "start here".
        // The system never tracks which layers the user expands.
        var elements = new List<StackPanel>();
        bool isFirst = true;

        foreach (var layer in layers)
        {
            var layerPanel = new StackPanel { Spacing = 2 };

            // Layer toggle — each layer independently expandable
            var layerToggle = new ToggleButton
            {
                Content = layer.Label,
                FontSize = 11,
                Padding = new Thickness(6, 2, 6, 2),
                MinWidth = 0,
                IsChecked = false,
                FontWeight = isFirst
                    ? Microsoft.UI.Text.FontWeights.Normal
                    : Microsoft.UI.Text.FontWeights.Normal
            };

            // First layer uses primary foreground, deeper layers use secondary
            if (!isFirst)
            {
                layerToggle.Foreground = (Microsoft.UI.Xaml.Media.Brush)
                    Application.Current.Resources["TextFillColorSecondaryBrush"];
            }

            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHeadingLevel(
                layerToggle, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level3);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                layerToggle, $"Expand {layer.Label} layer");

            // Content items — collapsed until toggle is checked
            var contentPanel = new StackPanel
            {
                Spacing = 2,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(8, 2, 0, 4)
            };

            var contentItems = layer.Content
                .Take(ExtensionBoundary.MaxContentPerLayer)
                .Select(c => c.Length > ExtensionBoundary.MaxLayerContentLength
                    ? c[..ExtensionBoundary.MaxLayerContentLength] + "…"
                    : c)
                .Where(c => !string.IsNullOrWhiteSpace(c));

            foreach (var item in contentItems)
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = $"• {item}",
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    IsTabStop = false,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)
                        Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
            }

            // Wire toggle to show/hide content
            layerToggle.Checked += (_, _) => contentPanel.Visibility = Visibility.Visible;
            layerToggle.Unchecked += (_, _) => contentPanel.Visibility = Visibility.Collapsed;

            layerPanel.Children.Add(layerToggle);
            layerPanel.Children.Add(contentPanel);
            elements.Add(layerPanel);

            isFirst = false;
        }

        LayersList.ItemsSource = elements;
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
