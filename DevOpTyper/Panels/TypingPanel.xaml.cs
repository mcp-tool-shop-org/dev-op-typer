using Microsoft.UI.Xaml.Controls;

namespace DevOpTyper.Panels;

public sealed partial class TypingPanel : UserControl
{
    public TypingPanel()
    {
        InitializeComponent();
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
        TypingBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }
}
