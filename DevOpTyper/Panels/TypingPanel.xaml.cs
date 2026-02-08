using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    /// </summary>
    public void SetTarget(string title, string language, string code)
    {
        SnippetTitle.Text = title ?? "Untitled";
        SnippetLanguage.Text = language ?? "unknown";
        TargetCode.Text = code ?? "";
    }

    /// <summary>
    /// Gets the currently typed text.
    /// </summary>
    public string TypedText => TypingBox.Text;

    /// <summary>
    /// Clears the typing box.
    /// </summary>
    public void ClearTyping()
    {
        TypingBox.Text = "";
    }

    /// <summary>
    /// Sets focus to the typing box.
    /// </summary>
    public void FocusTypingBox()
    {
        TypingBox.Focus(FocusState.Programmatic);
    }
}
