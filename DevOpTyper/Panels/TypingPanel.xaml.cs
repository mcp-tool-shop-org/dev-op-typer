using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

public sealed partial class TypingPanel : UserControl
{
    public event RoutedEventHandler? StartClicked;
    public event RoutedEventHandler? ResetClicked;
    public event RoutedEventHandler? SkipClicked;
    public event TextChangedEventHandler? TypingTextChanged;

    /// <summary>
    /// Fired when the user clicks the action button on the completion banner.
    /// The sender carries the action tag string.
    /// </summary>
    public event EventHandler<string>? CompletionActionClicked;

    /// <summary>
    /// All intent chips mapped to their UserIntent value.
    /// </summary>
    private readonly (ToggleButton Chip, UserIntent Intent)[] _intentChips;

    public TypingPanel()
    {
        InitializeComponent();

        StartButton.Click += (s, e) => StartClicked?.Invoke(s, e);
        ResetButton.Click += (s, e) => ResetClicked?.Invoke(s, e);
        SkipButton.Click += (s, e) => SkipClicked?.Invoke(s, e);
        TypingBox.TextChanged += (s, e) => TypingTextChanged?.Invoke(s, e);

        CompletionDismissButton.Click += (_, _) => DismissCompletionBanner();
        CompletionActionButton.Click += (_, _) =>
        {
            var tag = CompletionActionButton.Tag as string ?? "";
            CompletionActionClicked?.Invoke(this, tag);
            DismissCompletionBanner();
        };

        // Wire intent chips — mutual exclusion (at most one selected)
        _intentChips = new[]
        {
            (IntentFocusChip, UserIntent.Focus),
            (IntentChallengeChip, UserIntent.Challenge),
            (IntentMaintenanceChip, UserIntent.Maintenance),
            (IntentExplorationChip, UserIntent.Exploration)
        };

        foreach (var (chip, _) in _intentChips)
        {
            chip.Checked += OnIntentChipChecked;
            chip.Unchecked += (_, _) => { }; // Allow unchecking freely
        }
    }

    /// <summary>
    /// When a chip is checked, uncheck all others (radio-like behavior).
    /// </summary>
    private void OnIntentChipChecked(object sender, RoutedEventArgs e)
    {
        foreach (var (chip, _) in _intentChips)
        {
            if (chip != sender)
                chip.IsChecked = false;
        }
    }

    /// <summary>
    /// Gets the user's declared intent, or null if none is selected.
    /// </summary>
    public UserIntent? SelectedUserIntent
    {
        get
        {
            foreach (var (chip, intent) in _intentChips)
            {
                if (chip.IsChecked == true)
                    return intent;
            }
            return null;
        }
    }

    /// <summary>
    /// Clears the user intent selection (no chip selected).
    /// </summary>
    public void ClearUserIntent()
    {
        foreach (var (chip, _) in _intentChips)
            chip.IsChecked = false;
    }

    /// <summary>
    /// Sets the target snippet to display.
    /// Renders the target as all-pending in the per-character renderer.
    /// </summary>
    public void SetTarget(string title, string language, string code)
    {
        SnippetTitle.Text = title ?? "Untitled";
        SnippetLanguage.Text = language ?? "unknown";

        // Render target text as all-pending in the live renderer
        CodeRenderer.RenderTarget(code ?? "");
    }

    /// <summary>
    /// Updates the per-character diff display.
    /// Call this on every DiffUpdated or ProgressUpdated event.
    /// </summary>
    /// <param name="diff">Character diff array from the typing engine.</param>
    /// <param name="cursorPosition">Index of current cursor position (first pending char).</param>
    public void UpdateDiff(CharDiff[] diff, int cursorPosition)
    {
        CodeRenderer.RenderDiff(diff, cursorPosition);
    }

    /// <summary>
    /// Gets the currently typed text.
    /// </summary>
    public string TypedText => TypingBox.Text;

    /// <summary>
    /// Clears the typing box and resets the renderer.
    /// </summary>
    public void ClearTyping()
    {
        TypingBox.Text = "";
        CodeRenderer.Clear();
    }

    /// <summary>
    /// Sets focus to the typing box.
    /// </summary>
    public void FocusTypingBox()
    {
        TypingBox.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Sets the typed text (used by hardcore mode text correction).
    /// </summary>
    public void SetTypedText(string text)
    {
        TypingBox.Text = text ?? "";
    }

    /// <summary>
    /// Shows the session completion banner with results and optional action.
    /// </summary>
    /// <param name="wpm">Final WPM.</param>
    /// <param name="accuracy">Final accuracy percentage.</param>
    /// <param name="xp">XP earned.</param>
    /// <param name="isPerfect">Whether the session had zero errors.</param>
    /// <param name="actionLabel">Optional action button label (null = no action button).</param>
    /// <param name="actionTag">Tag to identify the action when clicked.</param>
    public void ShowCompletionBanner(
        double wpm, double accuracy, int xp, bool isPerfect,
        string? actionLabel = null, string? actionTag = null)
    {
        string title = isPerfect ? "Perfect!" : "Session Complete!";
        CompletionTitle.Text = title;
        CompletionStats.Text = $"{wpm:F0} WPM | {accuracy:F0}% accuracy | +{xp} XP";

        if (!string.IsNullOrEmpty(actionLabel))
        {
            CompletionActionButton.Content = actionLabel;
            CompletionActionButton.Tag = actionTag ?? "";
            CompletionActionButton.Visibility = Visibility.Visible;
        }
        else
        {
            CompletionActionButton.Visibility = Visibility.Collapsed;
        }

        CompletionBanner.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the completion banner.
    /// </summary>
    public void DismissCompletionBanner()
    {
        CompletionBanner.Visibility = Visibility.Collapsed;
    }
}
