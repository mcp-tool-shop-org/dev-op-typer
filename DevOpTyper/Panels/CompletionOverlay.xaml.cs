using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Numerics;

namespace DevOpTyper.Panels;

/// <summary>
/// Overlay panel that displays completion results with animation.
/// </summary>
public sealed partial class CompletionOverlay : UserControl
{
    public event EventHandler? ContinueClicked;

    private readonly Compositor _compositor;

    public CompletionOverlay()
    {
        InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    /// <summary>
    /// Shows the completion overlay with the given results.
    /// </summary>
    public void Show(double wpm, double accuracy, int xp, bool isPerfect)
    {
        // Update display
        WpmText.Text = $"WPM: {wpm:F0}";
        AccuracyText.Text = $"Accuracy: {accuracy:F1}%";
        XpText.Text = $"+{xp} XP";

        // Set status based on performance
        if (isPerfect)
        {
            StatusIcon.Glyph = "\uE735"; // Star
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gold);
            TitleText.Text = "Perfect!";
        }
        else if (accuracy >= 95)
        {
            StatusIcon.Glyph = "\uE73E"; // Checkmark
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
            TitleText.Text = "Excellent!";
        }
        else if (accuracy >= 85)
        {
            StatusIcon.Glyph = "\uE73E"; // Checkmark
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
            TitleText.Text = "Good Job!";
        }
        else
        {
            StatusIcon.Glyph = "\uE8FB"; // Refresh/retry icon
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
            TitleText.Text = "Keep Practicing!";
        }

        // Make visible
        Visibility = Visibility.Visible;

        // Animate in
        AnimateIn();
    }

    /// <summary>
    /// Hides the completion overlay.
    /// </summary>
    public void Hide()
    {
        AnimateOut();
    }

    private void AnimateIn()
    {
        var visual = ElementCompositionPreview.GetElementVisual(ContentCard);
        
        // Scale animation
        var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0f, new Vector3(0.8f, 0.8f, 1f));
        scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(300);

        // Fade animation
        var fadeAnimation = _compositor.CreateScalarKeyFrameAnimation();
        fadeAnimation.InsertKeyFrame(0f, 0f);
        fadeAnimation.InsertKeyFrame(1f, 1f);
        fadeAnimation.Duration = TimeSpan.FromMilliseconds(300);

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation("Opacity", fadeAnimation);
    }

    private void AnimateOut()
    {
        var visual = ElementCompositionPreview.GetElementVisual(ContentCard);

        // Scale animation
        var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
        scaleAnimation.InsertKeyFrame(1f, new Vector3(0.8f, 0.8f, 1f));
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(200);

        // Fade animation
        var fadeAnimation = _compositor.CreateScalarKeyFrameAnimation();
        fadeAnimation.InsertKeyFrame(0f, 1f);
        fadeAnimation.InsertKeyFrame(1f, 0f);
        fadeAnimation.Duration = TimeSpan.FromMilliseconds(200);

        // Create batch for completion callback
        var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Visibility = Visibility.Collapsed;
            });
        };

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation("Opacity", fadeAnimation);

        batch.End();
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        ContinueClicked?.Invoke(this, EventArgs.Empty);
    }
}
