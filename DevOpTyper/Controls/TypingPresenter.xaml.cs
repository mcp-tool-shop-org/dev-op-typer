using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using DevOpTyper.Models;

namespace DevOpTyper.Controls;

/// <summary>
/// Renders per-character typing feedback using a RichTextBlock.
/// Each character is styled based on its CharDiff state:
///   - Correct: accent green, normal weight
///   - Error: red, bold, underlined (non-color cue for accessibility)
///   - Pending: muted gray
///   - Caret: current position highlighted with background
///   - Extra: red, strikethrough
/// </summary>
public sealed partial class TypingPresenter : UserControl
{
    // Cached brushes (resolved once from theme resources)
    private SolidColorBrush? _correctBrush;
    private SolidColorBrush? _errorBrush;
    private SolidColorBrush? _pendingBrush;
    private SolidColorBrush? _caretBrush;
    private SolidColorBrush? _extraBrush;

    // Track previous state to avoid unnecessary full rebuilds
    private int _lastTypedLength = -1;
    private int _lastDiffLength = -1;
    private bool _highContrast;

    public TypingPresenter()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ResolveBrushes();
    }

    /// <summary>
    /// Enable/disable high-contrast mode (uses stronger colors).
    /// </summary>
    public bool HighContrast
    {
        get => _highContrast;
        set
        {
            if (_highContrast != value)
            {
                _highContrast = value;
                ResolveBrushes();
            }
        }
    }

    /// <summary>
    /// Renders the typing prompt with per-character diff states.
    /// This is the primary entry point — call on every DiffUpdated event.
    /// </summary>
    /// <param name="diff">Character diff array from the typing engine.</param>
    /// <param name="cursorPosition">Index of the current cursor position (first pending char).</param>
    public void RenderDiff(CharDiff[] diff, int cursorPosition = -1)
    {
        if (diff == null || diff.Length == 0)
        {
            PromptBlock.Blocks.Clear();
            _lastTypedLength = -1;
            _lastDiffLength = -1;
            return;
        }

        ResolveBrushesIfNeeded();

        // Rebuild the RichTextBlock content
        PromptBlock.Blocks.Clear();
        var paragraph = new Paragraph();

        // Batch consecutive chars with same state into single Run for perf
        int i = 0;
        while (i < diff.Length)
        {
            var state = diff[i].State;
            bool isCaret = (i == cursorPosition);

            // If this is the caret position, render it as a single char run
            if (isCaret)
            {
                var caretRun = CreateCharRun(diff[i], isCaret: true);
                paragraph.Inlines.Add(caretRun);
                i++;
                continue;
            }

            // Batch consecutive chars with same state
            int batchStart = i;
            while (i < diff.Length && diff[i].State == state && i != cursorPosition)
            {
                i++;
            }

            // Create a single run for the batch
            if (i - batchStart == 1)
            {
                paragraph.Inlines.Add(CreateCharRun(diff[batchStart], isCaret: false));
            }
            else
            {
                var batchRun = CreateBatchRun(diff, batchStart, i, state);
                paragraph.Inlines.Add(batchRun);
            }
        }

        PromptBlock.Blocks.Add(paragraph);

        // Track state for future optimization
        _lastDiffLength = diff.Length;
        _lastTypedLength = cursorPosition;

        // Auto-scroll to keep cursor in view
        if (cursorPosition >= 0)
        {
            ScrollToCaret(cursorPosition, diff.Length);
        }
    }

    /// <summary>
    /// Renders plain target text as all-pending (before typing starts).
    /// </summary>
    public void RenderTarget(string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            PromptBlock.Blocks.Clear();
            return;
        }

        ResolveBrushesIfNeeded();

        PromptBlock.Blocks.Clear();
        var paragraph = new Paragraph();

        // Render entire target as muted/pending
        var run = new Run
        {
            Text = MakeWhitespaceVisible(target),
            Foreground = _pendingBrush
        };
        paragraph.Inlines.Add(run);

        PromptBlock.Blocks.Add(paragraph);
        _lastTypedLength = -1;
        _lastDiffLength = -1;
    }

    /// <summary>
    /// Clears all rendered content.
    /// </summary>
    public void Clear()
    {
        PromptBlock.Blocks.Clear();
        _lastTypedLength = -1;
        _lastDiffLength = -1;
    }

    #region Run Creation

    private Run CreateCharRun(CharDiff diff, bool isCaret)
    {
        char displayChar = GetDisplayChar(diff);
        var run = new Run { Text = displayChar.ToString() };

        if (isCaret)
        {
            // Caret position: highlighted background via InlineUIContainer won't work
            // in RichTextBlock, so use a distinctive foreground color + bold
            run.Foreground = _caretBrush;
            run.FontWeight = FontWeights.Bold;
            // Underline acts as cursor indicator
            run.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
        }
        else
        {
            ApplyStateStyle(run, diff.State);
        }

        return run;
    }

    private Run CreateBatchRun(CharDiff[] diff, int start, int end, CharState state)
    {
        // Build text for the batch
        var chars = new char[end - start];
        for (int j = start; j < end; j++)
        {
            chars[j - start] = GetDisplayChar(diff[j]);
        }

        var run = new Run { Text = new string(chars) };
        ApplyStateStyle(run, state);
        return run;
    }

    private void ApplyStateStyle(Run run, CharState state)
    {
        switch (state)
        {
            case CharState.Correct:
                run.Foreground = _correctBrush;
                break;

            case CharState.Error:
                run.Foreground = _errorBrush;
                run.FontWeight = FontWeights.Bold;
                // Non-color accessibility cue: underline for errors
                run.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
                break;

            case CharState.Extra:
                run.Foreground = _extraBrush;
                run.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
                break;

            case CharState.Pending:
            default:
                run.Foreground = _pendingBrush;
                break;
        }
    }

    #endregion

    #region Display Helpers

    /// <summary>
    /// Makes whitespace characters visible with special symbols.
    /// </summary>
    private static char GetDisplayChar(CharDiff diff)
    {
        char c = diff.Expected;

        // Show whitespace characters visibly when they're errors
        if (diff.State == CharState.Error)
        {
            return c switch
            {
                ' ' => '\u00B7',  // Middle dot for space errors
                '\t' => '\u2192', // Right arrow for tab errors
                '\r' => '\u21B5', // Down-left arrow for CR errors
                '\n' => '\u21B5', // Down-left arrow for LF errors
                _ => c
            };
        }

        // For pending/correct, show regular chars but make newlines visible
        return c switch
        {
            '\n' => '\u21B5', // Show line break symbol, then actual break handled by wrapping
            '\r' => '\u200B', // Zero-width space (CR in CR+LF pair — invisible)
            '\t' => ' ',      // Show tab as space (Consolas renders tabs weird in RichTextBlock)
            _ => c
        };
    }

    /// <summary>
    /// Makes whitespace visible in a plain target string.
    /// </summary>
    private static string MakeWhitespaceVisible(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                '\n' => '\u21B5',
                '\r' => '\u200B',
                '\t' => ' ',
                _ => c
            });
        }
        return sb.ToString();
    }

    #endregion

    #region Auto-Scroll

    private void ScrollToCaret(int cursorPosition, int totalLength)
    {
        if (totalLength <= 0) return;

        // Estimate scroll position based on character ratio
        double ratio = (double)cursorPosition / totalLength;
        double scrollHeight = PromptScroller.ScrollableHeight;

        if (scrollHeight > 0)
        {
            // Scroll to keep cursor roughly in the middle third of the view
            double targetOffset = ratio * scrollHeight;

            // Only scroll if cursor would be out of view
            double currentTop = PromptScroller.VerticalOffset;
            double viewHeight = PromptScroller.ViewportHeight;

            // If cursor position estimate is below visible area, scroll down
            if (targetOffset > currentTop + viewHeight * 0.7)
            {
                PromptScroller.ChangeView(null, targetOffset - viewHeight * 0.3, null, disableAnimation: false);
            }
            // If cursor position estimate is above visible area, scroll up
            else if (targetOffset < currentTop + viewHeight * 0.2)
            {
                PromptScroller.ChangeView(null, Math.Max(0, targetOffset - viewHeight * 0.3), null, disableAnimation: false);
            }
        }
    }

    #endregion

    #region Brush Resolution

    private void ResolveBrushesIfNeeded()
    {
        if (_correctBrush == null)
        {
            ResolveBrushes();
        }
    }

    private void ResolveBrushes()
    {
        try
        {
            var resources = App.Current.Resources;

            // Correct: accent green (teal) — typed correctly
            _correctBrush = resources["DotAccent1Brush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.LightGreen);

            // Error: red — typed incorrectly
            _errorBrush = resources["DotErrorBrush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.Red);

            // Pending: muted gray — not yet typed
            _pendingBrush = resources["DotTextMutedBrush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.Gray);

            // Caret: bright accent blue — current position
            _caretBrush = resources["DotAccent2Brush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.CornflowerBlue);

            // Extra: same as error but with strikethrough
            _extraBrush = resources["DotErrorBrush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.Red);
        }
        catch
        {
            // Fallback if resources not available
            _correctBrush = new SolidColorBrush(Colors.LightGreen);
            _errorBrush = new SolidColorBrush(Colors.Red);
            _pendingBrush = new SolidColorBrush(Colors.Gray);
            _caretBrush = new SolidColorBrush(Colors.CornflowerBlue);
            _extraBrush = new SolidColorBrush(Colors.Red);
        }
    }

    #endregion
}
