using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using DevOpTyper.Services;
using DevOpTyper.Models;

namespace DevOpTyper;

public sealed partial class MainWindow : Window
{
    private readonly TypingEngine _typingEngine = new();
    private readonly SnippetService _snippetService = new();
    private readonly SmartSnippetSelector _smartSelector;
    private readonly PersistenceService _persistenceService = new();
    private readonly AudioService _audioService = new();
    private readonly KeyboardSoundHandler _keyboardSound;
    private readonly UiFeedbackService _uiFeedback;
    private Profile _profile = new();
    private AppSettings _settings = new();
    private bool _settingsPanelOpen = false;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        // Set the drag area to just the title text — buttons stay fully interactive
        // Without this, the entire title bar row is a drag region and eats button clicks
        SetTitleBar(TitleBarDragArea);

        SetWindowSize(1200, 760);

        // Initialize services
        _snippetService.Initialize();
        _smartSelector = new SmartSnippetSelector(_snippetService);

        // Initialize audio
        _audioService.Initialize();
        _keyboardSound = new KeyboardSoundHandler(_audioService);
        _uiFeedback = new UiFeedbackService(_audioService);

        // Load persisted data (profile + settings)
        var persisted = _persistenceService.Load();
        _profile = persisted.Profile;
        _settings = persisted.Settings;

        // Wire up settings panel events FIRST (before any population or audio init)
        SettingsPanel.AmbientVolumeChanged += (_, val) => _audioService.SetAmbientVolume(val);
        SettingsPanel.KeyboardVolumeChanged += (_, val) => _audioService.SetKeyboardVolume(val);
        SettingsPanel.UiVolumeChanged += (_, val) => _audioService.SetUiVolume(val);
        SettingsPanel.KeyboardThemeChanged += (_, theme) => _audioService.SwitchKeyboardTheme(theme);
        SettingsPanel.SoundscapeChanged += (_, scape) => _audioService.SwitchSoundscape(scape);

        // Restore saved audio settings
        _audioService.SetVolumes(_settings.AmbientVolume, _settings.KeyboardVolume, _settings.UiClickVolume);
        _audioService.SwitchKeyboardTheme(_settings.KeyboardSoundTheme);
        _audioService.SwitchSoundscape(_settings.SelectedSoundscape);

        // Populate dynamic dropdowns from discovered audio content
        SettingsPanel.PopulateThemes(_audioService.AvailableThemes, _audioService.CurrentTheme);
        SettingsPanel.PopulateSoundscapes(_audioService.AvailableSoundscapes, _audioService.CurrentSoundscape);

        // Start ambient audio (deferred to avoid blocking UI thread — MCI play takes ~1s)
        // Play first track in the soundscape — stays the same until user hits random button
        DispatcherQueue.TryEnqueue(() => _audioService.PlayAmbientTrack(0));

        // Wire up typing engine events
        _typingEngine.ProgressUpdated += OnTypingProgress;
        _typingEngine.SessionCompleted += OnSessionCompleted;
        _typingEngine.TextCorrected += OnTextCorrected;

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
        var typed = TypingPanel.TypedText;

        // Always play keyboard sound on any text change
        _keyboardSound.OnTextChanged(typed);

        if (_typingEngine.IsRunning)
        {
            _typingEngine.UpdateTypedText(typed, SettingsPanel.IsHardcoreMode);
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

            // Save profile + current audio settings
            _persistenceService.Save(new PersistedBlob
            {
                Profile = _profile,
                Settings = GetCurrentSettings()
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
        if (_settingsPanelOpen)
        {
            SettingsPanel.Visibility = Visibility.Visible;
            SettingsColumn.Width = new GridLength(280);
        }
        else
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            SettingsColumn.Width = new GridLength(0);
        }
    }

    private void AmbientRandomButton_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        _audioService.PlayRandomAmbient();
        UpdateAmbientMuteButton(false);
    }

    private bool _ambientMuted = false;

    private void AmbientMuteButton_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        _ambientMuted = !_ambientMuted;
        if (_ambientMuted)
        {
            _audioService.PauseAmbient();
        }
        else
        {
            _audioService.ResumeAmbient();
        }
        UpdateAmbientMuteButton(_ambientMuted);
    }

    private void UpdateAmbientMuteButton(bool muted)
    {
        _ambientMuted = muted;
        AmbientMuteButton.Content = muted ? "🔇 Muted" : "🔊 Ambient";
    }

    private AppSettings GetCurrentSettings()
    {
        _settings.AmbientVolume = SettingsPanel.AmbientVolume;
        _settings.KeyboardVolume = SettingsPanel.KeyboardVolume;
        _settings.UiClickVolume = SettingsPanel.UiVolume;
        _settings.KeyboardSoundTheme = SettingsPanel.SelectedKeyboardTheme;
        _settings.SelectedSoundscape = SettingsPanel.SelectedSoundscape;
        _settings.HardcoreMode = SettingsPanel.IsHardcoreMode;
        _settings.HighContrast = SettingsPanel.IsHighContrast;
        _settings.ReducedMotion = SettingsPanel.IsReducedMotion;
        return _settings;
    }
}
