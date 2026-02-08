using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using DevOpTyper.ViewModels;
using DevOpTyper.Services;
using DevOpTyper.Models;

namespace DevOpTyper;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly TypingEngine _typingEngine = new();
    private readonly SnippetService _snippetService = new();
    private bool _settingsPanelOpen = false;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetWindowSize(1200, 760);

        // Wire up typing engine events
        _typingEngine.ProgressUpdated += OnTypingProgress;
        _typingEngine.SessionCompleted += OnSessionCompleted;

        // Wire up UI events
        TypingPanel.StartClicked += StartTest_Click;
        TypingPanel.ResetClicked += ResetTest_Click;
        TypingPanel.SkipClicked += SkipTest_Click;
        TypingPanel.TypingTextChanged += TypingBox_TextChanged;

        // Initial state
        UpdateLevelBadge();
        LoadNewSnippet();
    }

    private void SetWindowSize(int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
    }

    private void LoadNewSnippet()
    {
        var language = SettingsPanel.SelectedLanguage;
        var snippet = _snippetService.GetRandomSnippet(language);
        
        if (snippet != null)
        {
            TypingPanel.SetTarget(snippet.Title ?? "Snippet", language, snippet.Code ?? "");
            _currentSnippet = snippet;
        }
        else
        {
            // Fallback if no snippets found
            TypingPanel.SetTarget("Hello World", language, "print('Hello, World!')");
            _currentSnippet = new Snippet { Title = "Hello World", Language = language, Code = "print('Hello, World!')" };
        }

        TypingPanel.ClearTyping();
        StatsPanel.Reset();
    }

    private Snippet? _currentSnippet;

    private void StartTest_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnippet != null)
        {
            _typingEngine.StartSession(_currentSnippet);
            TypingPanel.FocusTypingBox();
        }
    }

    private void ResetTest_Click(object sender, RoutedEventArgs e)
    {
        _typingEngine.Reset();
        TypingPanel.ClearTyping();
        StatsPanel.Reset();
        
        if (_currentSnippet != null)
        {
            _typingEngine.StartSession(_currentSnippet);
        }
        TypingPanel.FocusTypingBox();
    }

    private void SkipTest_Click(object sender, RoutedEventArgs e)
    {
        _typingEngine.CancelSession();
        LoadNewSnippet();
    }

    private void TypingBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_typingEngine.IsRunning)
        {
            var typed = TypingPanel.TypedText;
            _typingEngine.UpdateTypedText(typed, SettingsPanel.IsHardcoreMode);
        }
    }

    private void OnTypingProgress(object? sender, TypingProgressEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            StatsPanel.UpdateStats(
                e.Wpm,
                e.Accuracy,
                e.ErrorCount,
                e.TypedLength,
                e.TargetLength,
                _typingEngine.XpEarned
            );
        });
    }

    private void OnSessionCompleted(object? sender, TypingResultEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Update XP display
            UpdateLevelBadge();

            // Load next snippet
            LoadNewSnippet();
        });
    }

    private void UpdateLevelBadge()
    {
        // TODO: Load from profile
        LevelBadge.Text = $"Lv 1 • {_typingEngine.XpEarned} XP";
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsPanelOpen = !_settingsPanelOpen;
        SettingsColumn.Width = _settingsPanelOpen ? new GridLength(280) : new GridLength(0);
    }
}
