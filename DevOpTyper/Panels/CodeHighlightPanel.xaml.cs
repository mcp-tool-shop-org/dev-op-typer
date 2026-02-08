using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

/// <summary>
/// Panel that displays code with character-by-character highlighting based on typing accuracy.
/// </summary>
public sealed partial class CodeHighlightPanel : UserControl
{
    private static readonly SolidColorBrush PendingBrush = new(Colors.Gray);
    private static readonly SolidColorBrush CorrectBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush ErrorBrush = new(Colors.Red);
    private static readonly SolidColorBrush ExtraBrush = new(Colors.Orange);
    private static readonly SolidColorBrush CursorBrush = new(Colors.Yellow);

    private string _targetCode = "";

    public CodeHighlightPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the target code to display.
    /// </summary>
    public void SetTargetCode(string code)
    {
        _targetCode = code ?? "";
        UpdateDisplay(Array.Empty<CharDiff>());
    }

    /// <summary>
    /// Updates the display with the current diff state.
    /// </summary>
    public void UpdateDisplay(CharDiff[] diff)
    {
        CodeDisplay.Blocks.Clear();

        var paragraph = new Paragraph();

        if (diff.Length == 0 && !string.IsNullOrEmpty(_targetCode))
        {
            // Show all as pending
            var run = new Run
            {
                Text = _targetCode,
                Foreground = PendingBrush
            };
            paragraph.Inlines.Add(run);
        }
        else
        {
            // Build character-by-character with coloring
            int cursorPos = -1;
            for (int i = 0; i < diff.Length; i++)
            {
                if (diff[i].State == CharState.Pending && cursorPos < 0)
                {
                    cursorPos = i;
                }
            }

            for (int i = 0; i < diff.Length; i++)
            {
                var charDiff = diff[i];
                var brush = GetBrushForState(charDiff.State);
                
                // Highlight cursor position
                bool isCursor = (i == cursorPos);

                string charText = charDiff.Expected.ToString();
                // Handle newlines and special chars
                if (charDiff.Expected == '\n')
                {
                    charText = "↵\n";
                }
                else if (charDiff.Expected == '\t')
                {
                    charText = "→   ";
                }
                else if (charDiff.Expected == ' ' && charDiff.State == CharState.Error)
                {
                    charText = "·"; // Show space errors visibly
                }

                var run = new Run
                {
                    Text = charText,
                    Foreground = brush
                };

                if (isCursor)
                {
                    run.FontWeight = FontWeights.Bold;
                }

                if (charDiff.State == CharState.Error)
                {
                    // Underline errors
                    var underline = new Underline();
                    underline.Inlines.Add(run);
                    paragraph.Inlines.Add(underline);
                }
                else
                {
                    paragraph.Inlines.Add(run);
                }
            }

            // Add any extra typed characters
            foreach (var charDiff in diff.Where(d => d.State == CharState.Extra))
            {
                var run = new Run
                {
                    Text = charDiff.Actual?.ToString() ?? "?",
                    Foreground = ExtraBrush
                };
                var strikethrough = new Span();
                // WinUI doesn't have Strikethrough, use different styling
                strikethrough.Inlines.Add(run);
                paragraph.Inlines.Add(strikethrough);
            }
        }

        CodeDisplay.Blocks.Add(paragraph);
    }

    private static SolidColorBrush GetBrushForState(CharState state) => state switch
    {
        CharState.Correct => CorrectBrush,
        CharState.Error => ErrorBrush,
        CharState.Extra => ExtraBrush,
        _ => PendingBrush
    };

    /// <summary>
    /// Resets to show target code as all pending.
    /// </summary>
    public void Reset()
    {
        UpdateDisplay(Array.Empty<CharDiff>());
    }
}
