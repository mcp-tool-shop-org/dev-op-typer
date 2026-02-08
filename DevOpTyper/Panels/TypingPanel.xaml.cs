using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevOpTyper.Models;

namespace DevOpTyper.Panels;

public sealed partial class TypingPanel : UserControl
{
    public event RoutedEventHandler? StartClicked;
    public event RoutedEventHandler? ResetClicked;
    public event RoutedEventHandler? SkipClicked;
    public event TextChangedEventHandler? TypingTextChanged;

    public TypingPanel()
    {
        InitializeComponent();

        StartButton.Click += (s, e) => StartClicked?.Invoke(s, e);
        ResetButton.Click += (s, e) => ResetClicked?.Invoke(s, e);
        SkipButton.Click += (s, e) => SkipClicked?.Invoke(s, e);
        TypingBox.TextChanged += (s, e) => TypingTextChanged?.Invoke(s, e);
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
}
