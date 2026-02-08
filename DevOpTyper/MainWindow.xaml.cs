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
    private readonly SmartSnippetSelector _smartSelector;
    private readonly PersistenceService _persistenceService = new();
    private readonly AudioService _audioService = new();
    private readonly KeyboardSoundHandler _keyboardSound;
    private readonly UiFeedbackService _uiFeedback;
    private Profile _profile = new();
    private bool _settingsPanelOpen = false;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetWindowSize(1200, 760);

        // Initialize services
        _snippetService.Initialize();
        _smartSelector = new SmartSnippetSelector(_snippetService);

        // Initialize audio
        _audioService.Initialize();
        _keyboardSound = new KeyboardSoundHandler(_audioService);
        _uiFeedback = new UiFeedbackService(_audioService);

        // Apply volume from settings panel defaults
        _audioService.SetVolumes(
            SettingsPanel.AmbientVolume,
            SettingsPanel.KeyboardVolume,
            SettingsPanel.UiVolume
        );

        // Start ambient audio
        _audioService.PlayRandomAmbient();

        // Load persisted profile
        var persisted = _persistenceService.Load();
        _profile = persisted.Profile;

        // Wire up typing engine events
        _typingEngine.ProgressUpdated += OnTypingProgress;
        _typingEngine.SessionCompleted += OnSessionCompleted;
        _typingEngine.TextCorrected += OnTextCorrected;

        // Wire up UI events
        TypingPanel.StartClicked += StartTest_Click;
        TypingPanel.ResetClicked += ResetTest_Click;
        TypingPanel.SkipClicked += SkipTest_Click;
        TypingPanel.TypingTextChanged += TypingBox_TextChanged;

        // Wire up settings panel volume sliders
        SettingsPanel.AmbientVolumeChanged += (_, val) => _audioService.SetAmbientVolume(val);
        SettingsPanel.KeyboardVolumeChanged += (_, val) => _audioService.SetKeyboardVolume(val);
        SettingsPanel.UiVolumeChanged += (_, val) => _audioService.SetUiVolume(val);

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

        // Use smart selection for better learning experience
        var snippet = _smartSelector.SelectNext(language, _profile);

        TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
        _currentSnippet = snippet;

        TypingPanel.ClearTyping();
        StatsPanel.Reset();
    }

    private void LoadSnippetForWeakChars()
    {
        var language = SettingsPanel.SelectedLanguage;

        if (_profile.WeakChars.Count > 0)
        {
            var snippet = _smartSelector.SelectForWeakChars(language, _profile, _profile.WeakChars);
            TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
            _currentSnippet = snippet;
            TypingPanel.ClearTyping();
            StatsPanel.Reset();
        }
        else
        {
            LoadNewSnippet();
        }
    }

    private Snippet? _currentSnippet;

    private void StartTest_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        if (_currentSnippet != null)
        {
            bool hardcore = SettingsPanel.IsHardcoreMode;
            _typingEngine.StartSession(_currentSnippet, hardcore);
            _keyboardSound.Reset();
            TypingPanel.FocusTypingBox();
        }
    }

    private void ResetTest_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        _typingEngine.Reset();
        TypingPanel.ClearTyping();
        StatsPanel.Reset();
        _keyboardSound.Reset();

        if (_currentSnippet != null)
        {
            bool hardcore = SettingsPanel.IsHardcoreMode;
            _typingEngine.StartSession(_currentSnippet, hardcore);
        }
        TypingPanel.FocusTypingBox();
    }

    private void SkipTest_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        _typingEngine.CancelSession();
        LoadNewSnippet();
    }

    private void TypingBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_typingEngine.IsRunning)
        {
            var typed = TypingPanel.TypedText;
            _typingEngine.UpdateTypedText(typed, SettingsPanel.IsHardcoreMode);

            // Play keyboard sound
            _keyboardSound.OnTextChanged(typed);
        }
    }

    private void OnTextCorrected(object? sender, string correctedText)
    {
        // Hardcore mode corrected the text - update the textbox
        DispatcherQueue.TryEnqueue(() =>
        {
            // This would need the TypingPanel to expose a SetTypedText method
            // For now, we'll skip this UI update
        });
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

            // Track weak chars from errors
            if (e.HasErrors && e.Diff.Length > 0)
            {
                foreach (var diff in e.Diff)
                {
                    if (diff.State == CharState.Error)
                    {
                        _profile.RecordWeakChar(diff.Expected);
                    }
                }
            }
        });
    }

    private void OnSessionCompleted(object? sender, TypingResultEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Play completion sound
            _keyboardSound.OnSessionComplete();

            // Update profile with results
            _profile.AddXp(e.XpEarned);

            if (_currentSnippet != null)
            {
                _profile.UpdateRating(_currentSnippet.Language, e.FinalAccuracy, e.FinalWpm);
            }

            // Save profile
            _persistenceService.Save(new PersistedBlob
            {
                Profile = _profile,
                Settings = new AppSettings()
            });

            // Update XP display
            UpdateLevelBadge();

            // Load next snippet
            LoadNewSnippet();
        });
    }

    private void UpdateLevelBadge()
    {
        LevelBadge.Text = $"Lv {_profile.Level} • {_profile.Xp} XP";
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        _settingsPanelOpen = !_settingsPanelOpen;
        SettingsColumn.Width = _settingsPanelOpen ? new GridLength(280) : new GridLength(0);
    }

    private void AmbientRandomButton_Click(object sender, RoutedEventArgs e)
    {
        _audioService.PlayRandomAmbient();
        UpdateAmbientMuteButton(false);
    }

    private bool _ambientMuted = false;

    private void AmbientMuteButton_Click(object sender, RoutedEventArgs e)
    {
        _ambientMuted = !_ambientMuted;
        if (_ambientMuted)
        {
            _audioService.StopAmbient();
        }
        else
        {
            _audioService.PlayRandomAmbient();
        }
        UpdateAmbientMuteButton(_ambientMuted);
    }

    private void UpdateAmbientMuteButton(bool muted)
    {
        _ambientMuted = muted;
        AmbientMuteButton.Content = muted ? "🔇 Muted" : "🔊 Ambient";
    }
}
