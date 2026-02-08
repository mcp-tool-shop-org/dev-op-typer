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
    private readonly TrendAnalyzer _trendAnalyzer = new();
    private readonly FatigueDetector _fatigueDetector = new();
    private readonly PracticeRecommender _recommender = new();
    private Profile _profile = new();
    private AppSettings _settings = new();
    private bool _settingsPanelOpen = false;
    private int _lastHeatmapIndex = 0; // Tracks how far we've recorded hits/misses

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
        _typingEngine.DiffUpdated += OnDiffUpdated;
        _typingEngine.SessionCompleted += OnSessionCompleted;
        _typingEngine.TextCorrected += OnTextCorrected;

        // Wire up UI events
        TypingPanel.StartClicked += StartTest_Click;
        TypingPanel.ResetClicked += ResetTest_Click;
        TypingPanel.SkipClicked += SkipTest_Click;
        TypingPanel.TypingTextChanged += TypingBox_TextChanged;

        // Restore typing rules UI from saved settings
        SettingsPanel.LoadTypingRules(_settings.TypingRules);

        // Initial state
        UpdateLevelBadge();
        RefreshAnalytics(persisted);
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
            var rules = SettingsPanel.GetTypingRules();

            // Compute repeat count for diminishing XP returns
            int repeats = _persistenceService.Load().History.Records
                .Count(r => r.SnippetId == _currentSnippet.Id);
            _typingEngine.RepeatCount = repeats;

            // Attach practice context with snapshot of current state
            var context = PracticeContext.Default();
            context.EffectiveDifficulty = _currentSnippet.Difficulty;
            context.RatingAtStart = _profile.GetRating(_currentSnippet.Language);
            if (repeats > 0) context.Intent = PracticeIntent.Repeat;

            _typingEngine.PracticeContext = context;
            _typingEngine.StartSession(_currentSnippet, hardcore, rules);
            _lastHeatmapIndex = 0;
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
        _lastHeatmapIndex = 0;

        if (_currentSnippet != null)
        {
            bool hardcore = SettingsPanel.IsHardcoreMode;
            var rules = SettingsPanel.GetTypingRules();
            _typingEngine.StartSession(_currentSnippet, hardcore, rules);
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

    private void OnDiffUpdated(object? sender, CharDiff[] diff)
    {
        // Forward diff updates to the per-character renderer on the UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            // Cursor = first pending character (i.e., where the user should type next)
            int cursorPos = -1;
            for (int i = 0; i < diff.Length; i++)
            {
                if (diff[i].State == CharState.Pending)
                {
                    cursorPos = i;
                    break;
                }
            }

            TypingPanel.UpdateDiff(diff, cursorPos);
        });
    }

    private void OnTextCorrected(object? sender, string correctedText)
    {
        // Hardcore mode corrected the text — update the textbox
        DispatcherQueue.TryEnqueue(() =>
        {
            TypingPanel.SetTypedText(correctedText);
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

            // Feed ONLY newly typed characters into the heatmap (not the entire diff).
            // _lastHeatmapIndex tracks where we've already recorded, so each char
            // is counted exactly once. This prevents inflated hit/miss counts and
            // reduces per-keystroke work from O(n) to O(1).
            if (e.Diff.Length > 0)
            {
                int typedSoFar = e.TypedLength;
                for (int i = _lastHeatmapIndex; i < typedSoFar && i < e.Diff.Length; i++)
                {
                    var diff = e.Diff[i];
                    if (diff.State == CharState.Error)
                    {
                        _profile.RecordMiss(diff.Expected, diff.Actual);
                    }
                    else if (diff.State == CharState.Correct)
                    {
                        _profile.RecordHit(diff.Expected);
                    }
                }
                _lastHeatmapIndex = typedSoFar;
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

            // Create and save session record
            var blob = _persistenceService.Load();
            if (_currentSnippet != null)
            {
                var record = SessionRecord.FromSession(
                    snippetId: _currentSnippet.Id,
                    language: _currentSnippet.Language,
                    snippetTitle: _currentSnippet.Title ?? "Unknown",
                    wpm: e.FinalWpm,
                    accuracy: e.FinalAccuracy,
                    errorCount: e.ErrorCount,
                    totalChars: _currentSnippet.Code?.Length ?? 0,
                    duration: _typingEngine.Elapsed,
                    difficulty: _currentSnippet.Difficulty,
                    xpEarned: e.XpEarned,
                    hardcoreMode: SettingsPanel.IsHardcoreMode,
                    context: e.Context
                );
                blob.History.AddRecord(record);

                // Feed longitudinal data (trend tracking, weakness snapshots)
                blob.Longitudinal.RecordSession(record);
                blob.Longitudinal.MaybeSnapshotWeakness(_currentSnippet.Language, _profile.Heatmap);

                // Track last practiced language
                blob.LastPracticedByLanguage[_currentSnippet.Language] = DateTime.UtcNow;
            }

            // Save profile + history + settings
            blob.Profile = _profile;
            blob.Settings = GetCurrentSettings();
            _persistenceService.Save(blob);

            // Update XP display and analytics
            UpdateLevelBadge();
            RefreshAnalytics(blob);

            // Load next snippet
            LoadNewSnippet();
        });
    }

    private void UpdateLevelBadge()
    {
        LevelBadge.Text = $"Lv {_profile.Level} • {_profile.Xp} XP";
    }

    /// <summary>
    /// Updates the StatsPanel weakness, history, trends, and suggestions from persisted data.
    /// </summary>
    private void RefreshAnalytics(PersistedBlob blob)
    {
        StatsPanel.UpdateWeakSpots(blob.Profile.Heatmap);
        StatsPanel.UpdateHistory(blob.History);

        // Trend analysis (Phase 2)
        var trends = _trendAnalyzer.AnalyzeAll(blob.Longitudinal);
        StatsPanel.UpdateTrends(trends);

        // Practice suggestions (Phase 2)
        var language = SettingsPanel.SelectedLanguage;
        var suggestions = _recommender.Suggest(blob, language);
        StatsPanel.UpdateSuggestions(suggestions);
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
        _settings.TypingRules = SettingsPanel.GetTypingRules();
        return _settings;
    }
}
