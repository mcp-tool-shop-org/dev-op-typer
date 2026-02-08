using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace DevOpTyper.Controls;

// Optional control: renders per-character correctness into a RichTextBlock.
// Not wired into UI by default to keep starter simple.
public sealed partial class TypingPresenter : UserControl
{
    public TypingPresenter()
    {
        InitializeComponent();
    }

    public void Render(string target, string typed)
    {
        target ??= "";
        typed ??= "";

        PromptBlock.Blocks.Clear();
        var p = new Paragraph();

        int len = target.Length;
        for (int i = 0; i < len; i++)
        {
            char ch = target[i];
            bool hasTyped = i < typed.Length;
            bool correct = hasTyped && typed[i] == ch;

            var run = new Run { Text = ch.ToString() };

            if (!hasTyped)
            {
                run.Foreground = (Brush)App.Current.Resources["DotTextMutedBrush"];
            }
            else if (correct)
            {
                run.Foreground = (Brush)App.Current.Resources["DotTextBrush"];
            }
            else
            {
                run.Foreground = (Brush)App.Current.Resources["DotErrorBrush"];
                run.TextDecorations = TextDecorations.Underline;
            }

            p.Inlines.Add(run);
        }

        PromptBlock.Blocks.Add(p);
    }
}
