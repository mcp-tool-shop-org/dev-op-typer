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
    /// Fired when the user submits a session note (v0.4.0).
    /// The payload is the note text. Fired on banner dismiss if a note was entered.
    /// </summary>
    public event EventHandler<string>? SessionNoteSubmitted;

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

        // "Add a note" link — reveals the note input field
        AddNoteLink.Click += (_, _) =>
        {
            AddNoteLink.Visibility = Visibility.Collapsed;
            NoteInput.Visibility = Visibility.Visible;
            NoteInput.Focus(FocusState.Programmatic);
        };

        // Dismiss guidance button — session-scoped only, no tracking
        DismissGuidanceButton.Click += (_, _) =>
        {
            GuidanceArea.Visibility = Visibility.Collapsed;
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
            if (!ReferenceEquals(chip, sender))
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
    /// Shows or hides the intent chip bar.
    /// </summary>
    public void SetIntentChipsVisible(bool visible)
    {
        IntentChipBar.Visibility = visible
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    /// <summary>
    /// Pre-selects a default intent chip.
    /// </summary>
    public void SetDefaultIntent(UserIntent intent)
    {
        foreach (var (chip, chipIntent) in _intentChips)
        {
            chip.IsChecked = chipIntent == intent;
        }
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
    /// Shows optional scaffold hints below the snippet title.
    /// Scaffolds are short learning aids that help users notice patterns.
    /// They fade (via opacity) as the user demonstrates competence.
    /// Null or empty hints hides the scaffold area.
    /// </summary>
    public void ShowScaffold(string[]? hints, double opacity = 1.0)
    {
        if (hints == null || hints.Length == 0 || opacity <= 0)
        {
            ScaffoldHints.Visibility = Visibility.Collapsed;
            return;
        }

        var capped = hints.Take(ExtensionBoundary.MaxScaffoldHints)
            .Select(h => h.Length > ExtensionBoundary.MaxScaffoldHintLength
                ? h[..ExtensionBoundary.MaxScaffoldHintLength] + "…"
                : h)
            .Where(h => !string.IsNullOrWhiteSpace(h));

        var text = string.Join(" · ", capped);
        if (string.IsNullOrWhiteSpace(text))
        {
            ScaffoldHints.Visibility = Visibility.Collapsed;
            return;
        }

        ScaffoldHints.Text = text;
        ScaffoldHints.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        ScaffoldHints.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Shows the pick reason text below the snippet metadata.
    /// Explains why the SessionPlanner chose this snippet (Target/Review/Stretch).
    /// Null or empty hides the text. Display-only — never affects engine behavior.
    /// </summary>
    public void ShowPickReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            PickReasonText.Visibility = Visibility.Collapsed;
            return;
        }

        PickReasonText.Text = reason;
        PickReasonText.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the guidance area. Called during active typing to remove distractions.
    /// </summary>
    public void HideGuidance()
    {
        GuidanceArea.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Shows contextual guidance notes from collective experience.
    /// Guidance notes use collective language ("often", "typically"),
    /// never directive ("you should"). Null hides the guidance area.
    /// Each snippet load re-shows guidance — dismissal is session-scoped
    /// (no persistence, no tracking, next snippet re-shows).
    /// </summary>
    public void ShowGuidance(GuidanceNote? guidance)
    {
        if (guidance == null || guidance.Notes.Length == 0)
        {
            GuidanceArea.Visibility = Visibility.Collapsed;
            return;
        }

        GuidanceItems.Items.Clear();

        var notes = guidance.Notes
            .Take(ExtensionBoundary.MaxGuidanceNotesPerSnippet)
            .Select(n => n.Length > ExtensionBoundary.MaxGuidanceNoteLength
                ? n[..ExtensionBoundary.MaxGuidanceNoteLength] + "…"
                : n)
            .Where(n => !string.IsNullOrWhiteSpace(n));

        foreach (var note in notes)
        {
            var tb = new TextBlock
            {
                Text = $"💡 {note}",
                FontSize = 10,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                IsTabStop = false
            };
            tb.Foreground = (Microsoft.UI.Xaml.Media.Brush)
                Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorTertiaryBrush"];
            GuidanceItems.Items.Add(tb);
        }

        GuidanceArea.Visibility = GuidanceItems.Items.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Shows a subtle community hint below the snippet title.
    /// Uses collective language — "typically" and "often", never "you should"
    /// or comparative framing. Null or empty signal hides the hint.
    /// </summary>
    public void ShowCommunityHint(AggregateSignal? signal)
    {
        if (signal == null)
        {
            CommunityHint.Visibility = Visibility.Collapsed;
            return;
        }

        var parts = new List<string>();

        if (signal.TypicalWpm.HasValue)
            parts.Add($"typically ~{signal.TypicalWpm.Value:F0} WPM");

        if (signal.TypicalAccuracy.HasValue)
            parts.Add($"~{signal.TypicalAccuracy.Value:F0}% accuracy");

        if (signal.CommonDifficulties.Length > 0)
            parts.Add($"often tricky: {string.Join(", ", signal.CommonDifficulties.Take(3))}");

        if (!string.IsNullOrWhiteSpace(signal.Hint))
            parts.Add(signal.Hint);

        if (parts.Count == 0)
        {
            CommunityHint.Visibility = Visibility.Collapsed;
            return;
        }

        CommunityHint.Text = string.Join(" · ", parts);
        CommunityHint.Visibility = Visibility.Visible;
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

        // Clear any previous retrospective and note
        RetrospectiveSection.Visibility = Visibility.Collapsed;
        RetrospectiveText.Text = "";
        NoteInput.Text = "";
        NoteInput.Visibility = Visibility.Collapsed;
        AddNoteLink.Visibility = Visibility.Visible;

        CompletionBanner.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Shows a brief retrospective beneath the completion banner.
    /// Contains factual observations only — no judgment or evaluation.
    /// </summary>
    /// <param name="lines">Lines of retrospective text to display.</param>
    public void ShowRetrospective(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            RetrospectiveSection.Visibility = Visibility.Collapsed;
            return;
        }

        RetrospectiveText.Text = string.Join("\n", lines);
        RetrospectiveSection.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the completion banner. If the user entered a note,
    /// fires SessionNoteSubmitted before hiding.
    /// </summary>
    public void DismissCompletionBanner()
    {
        // Capture and submit any note the user typed
        var note = NoteInput.Text?.Trim();
        if (!string.IsNullOrEmpty(note))
        {
            SessionNoteSubmitted?.Invoke(this, note);
        }

        // Reset note section for next session
        NoteInput.Text = "";
        NoteInput.Visibility = Visibility.Collapsed;
        AddNoteLink.Visibility = Visibility.Visible;

        CompletionBanner.Visibility = Visibility.Collapsed;
        RetrospectiveSection.Visibility = Visibility.Collapsed;
    }
}
