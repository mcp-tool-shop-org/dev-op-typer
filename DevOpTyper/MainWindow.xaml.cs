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
    private readonly ContentLibraryService _contentLibraryService = new();
    private readonly SmartSnippetSelector _smartSelector;
    private readonly PersistenceService _persistenceService = new();
    private readonly AudioService _audioService = new();
    private readonly KeyboardSoundHandler _keyboardSound;
    private readonly UiFeedbackService _uiFeedback;
    private readonly TrendAnalyzer _trendAnalyzer = new();
    private readonly FatigueDetector _fatigueDetector = new();
    private readonly PracticeRecommender _recommender = new();
    private readonly AdaptiveDifficultyEngine _adaptiveDifficulty = new();
    private readonly SessionPacer _sessionPacer = new();
    private readonly WeaknessTracker _weaknessTracker = new();
    private readonly PracticeConfigService _practiceConfigService = new();
    private readonly CommunitySignalService _communitySignals = new();
    private readonly GuidanceService _guidanceService = new();
    private Profile _profile = new();
    private AppSettings _settings = new();
    private bool _settingsPanelOpen = false;
    private int _lastHeatmapIndex = 0; // Tracks how far we've recorded hits/misses
    private PracticeContext? _pendingContext; // Set by action handlers, consumed by StartTest

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        // Set the drag area to just the title text — buttons stay fully interactive
        // Without this, the entire title bar row is a drag region and eats button clicks
        SetTitleBar(TitleBarDragArea);

        SetWindowSize(1200, 760);

        // Initialize services
        _contentLibraryService.Initialize();
        _smartSelector = new SmartSnippetSelector(_contentLibraryService);

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

        // Wire up suggestion and weakness practice events
        StatsPanel.SuggestionFollowed += OnSuggestionFollowed;
        StatsPanel.WeaknessPracticeRequested += OnWeaknessPracticeRequested;
        TypingPanel.CompletionActionClicked += OnCompletionActionClicked;

        // Wire up session note events
        TypingPanel.SessionNoteSubmitted += OnSessionNoteSubmitted;

        // Restore typing rules UI from saved settings
        SettingsPanel.LoadTypingRules(_settings.TypingRules);

        // Restore practice preferences
        SettingsPanel.LoadPracticePreferences(_settings);
        ApplyPracticePreferences();

        // User snippets status
        SettingsPanel.UpdateUserSnippetStatus(_contentLibraryService.UserContent);
        SettingsPanel.OpenUserSnippetsFolderRequested += OnOpenUserSnippetsFolder;

        // Export/import bundle + community folder
        SettingsPanel.WireBundleButtons();
        SettingsPanel.ExportBundleRequested += OnExportBundle;
        SettingsPanel.ImportBundleRequested += OnImportBundle;
        SettingsPanel.OpenCommunityFolderRequested += OnOpenCommunityFolder;

        // Community content status (updated again after guidance init — see below)
        SettingsPanel.UpdateCommunityContentStatus(_contentLibraryService.CommunityContent);

        // Community signals toggle
        SettingsPanel.CommunitySignalsChanged += (_, enabled) =>
        {
            _settings.ShowCommunitySignals = enabled;
            // Immediately update the current hint visibility
            if (_currentSnippet != null)
            {
                if (enabled)
                {
                    var signal = _communitySignals.GetSignal(_currentSnippet.Id);
                    TypingPanel.ShowCommunityHint(signal);
                }
                else
                {
                    TypingPanel.ShowCommunityHint(null);
                }
            }
        };

        // Scaffold hints toggle (v0.8.0)
        SettingsPanel.ScaffoldsChanged += (_, enabled) =>
        {
            _settings.ShowScaffolds = enabled;
            if (_currentSnippet != null)
            {
                if (enabled)
                {
                    var blob = _persistenceService.Load();
                    var opacity = ScaffoldFadeService.ComputeOpacity(_currentSnippet.Id, blob.History);
                    TypingPanel.ShowScaffold(_currentSnippet.Scaffolds, opacity);
                }
                else
                {
                    TypingPanel.ShowScaffold(null);
                }
            }
        };

        // Demonstrations toggle (v0.8.0)
        SettingsPanel.DemonstrationsChanged += (_, enabled) =>
        {
            _settings.ShowDemonstrations = enabled;
            if (_currentSnippet != null)
            {
                if (enabled)
                    DemonstrationPanel.SetSnippet(_currentSnippet);
                else
                    DemonstrationPanel.Hide();
            }
        };

        // Guidance toggle (v0.8.0)
        SettingsPanel.GuidanceChanged += (_, enabled) =>
        {
            _settings.ShowGuidance = enabled;
            if (_currentSnippet != null)
            {
                if (enabled)
                {
                    var guidance = _guidanceService.GetGuidance(_currentSnippet.Id);
                    TypingPanel.ShowGuidance(guidance);
                }
                else
                {
                    TypingPanel.ShowGuidance(null);
                }
            }
        };

        // Skill layers toggle (v0.8.0)
        SettingsPanel.SkillLayersChanged += (_, enabled) =>
        {
            _settings.ShowSkillLayers = enabled;
            if (_currentSnippet != null)
            {
                if (enabled)
                    LayersPanel.SetSnippet(_currentSnippet);
                else
                    LayersPanel.Hide();
            }
        };

        // Community signals (display-only — never affects frozen services)
        _communitySignals.Initialize();

        // Guidance notes (display-only — never affects frozen services)
        _guidanceService.Initialize();

        // Refresh community status now that guidance is loaded
        SettingsPanel.UpdateCommunityContentStatus(
            _contentLibraryService.CommunityContent, _guidanceService.GuidanceCount);

        // Practice configs
        _practiceConfigService.Initialize();
        SettingsPanel.PopulateConfigs(
            _practiceConfigService.GetConfigNames(),
            _settings.SelectedPracticeConfig);
        UpdateConfigDescription();
        SettingsPanel.UpdateConfigErrors(_practiceConfigService.LoadErrors);
        SettingsPanel.PracticeConfigChanged += OnPracticeConfigChanged;

        // Session pacing
        _sessionPacer.OnAppLaunched();

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
        var config = GetActivePracticeConfig();

        // Config can override language
        if (!string.IsNullOrEmpty(config.Language))
            language = config.Language;

        Snippet snippet;
        if (SettingsPanel.IsAdaptiveDifficulty)
        {
            // Adaptive: use trend-aware difficulty and trajectory scoring
            var blob = _persistenceService.Load();
            var difficultyProfile = _adaptiveDifficulty.ComputeDifficulty(
                language, _profile, blob.Longitudinal);

            // Apply config difficulty bias
            if (config.HasDifficultyBias)
            {
                difficultyProfile.TargetDifficulty = Math.Clamp(
                    difficultyProfile.TargetDifficulty + config.DifficultyOffset, 1, 5);
            }

            var weaknessReport = _weaknessTracker.GetReport(
                language, _profile.Heatmap, blob.Longitudinal);

            snippet = _smartSelector.SelectAdaptive(
                language, _profile, difficultyProfile, weaknessReport);
        }
        else
        {
            // Non-adaptive: use basic skill-rated selection
            snippet = _smartSelector.SelectNext(language, _profile);
        }

        TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
        _currentSnippet = snippet;

        TypingPanel.ClearTyping();
        StatsPanel.Reset();

        // Show explanatory perspectives (if any) between sessions
        ExplanationPanel.SetSnippet(snippet);

        // Show alternative approaches (if any) between sessions
        if (_settings.ShowDemonstrations)
            DemonstrationPanel.SetSnippet(snippet);
        else
            DemonstrationPanel.Hide();

        // Show community signal hint (if available and enabled)
        if (_settings.ShowCommunitySignals)
        {
            var signal = _communitySignals.GetSignal(snippet.Id);
            TypingPanel.ShowCommunityHint(signal);
        }
        else
        {
            TypingPanel.ShowCommunityHint(null);
        }

        // Show scaffold hints (if available and enabled)
        // Scaffolds fade as the user demonstrates competence with this snippet
        if (_settings.ShowScaffolds)
        {
            var scaffoldBlob = _persistenceService.Load();
            var opacity = ScaffoldFadeService.ComputeOpacity(snippet.Id, scaffoldBlob.History);
            TypingPanel.ShowScaffold(snippet.Scaffolds, opacity);
        }
        else
        {
            TypingPanel.ShowScaffold(null);
        }

        // Show guidance notes (if available and enabled)
        // Guidance emerges from collective experience — always dismissible
        if (_settings.ShowGuidance)
        {
            var guidance = _guidanceService.GetGuidance(snippet.Id);
            TypingPanel.ShowGuidance(guidance);
        }
        else
        {
            TypingPanel.ShowGuidance(null);
        }

        // Show skill layers (if any and enabled) between sessions
        if (_settings.ShowSkillLayers)
            LayersPanel.SetSnippet(snippet);
        else
            LayersPanel.Hide();

        // Soft session frame — show typical range for this language
        StatsPanel.UpdateSessionFrame(_persistenceService.Load().Longitudinal, language);
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
        TypingPanel.DismissCompletionBanner();
        if (_currentSnippet != null)
        {
            bool hardcore = SettingsPanel.IsHardcoreMode;
            var rules = SettingsPanel.GetTypingRules();

            // Apply practice config overrides to typing rules
            var config = GetActivePracticeConfig();
            rules = config.ApplyTo(rules);

            // Compute repeat count for diminishing XP returns
            int repeats = _persistenceService.Load().History.Records
                .Count(r => r.SnippetId == _currentSnippet.Id);
            _typingEngine.RepeatCount = repeats;

            // Attach practice context — use pending context from action handlers, or default
            var context = _pendingContext ?? PracticeContext.Default();
            _pendingContext = null; // Consume it
            context.EffectiveDifficulty = _currentSnippet.Difficulty;
            context.RatingAtStart = _profile.GetRating(_currentSnippet.Language);
            if (repeats > 0 && context.Intent == PracticeIntent.Freeform)
                context.Intent = PracticeIntent.Repeat;

            // Capture user-declared intent — purely descriptive, no scoring impact
            context.DeclaredIntent = TypingPanel.SelectedUserIntent;

            // Apply focus area from settings if no other focus is set
            if (string.IsNullOrEmpty(context.Focus))
                context.Focus = SettingsPanel.FocusArea;

            _typingEngine.PracticeContext = context;
            _typingEngine.StartSession(_currentSnippet, hardcore, rules);
            _sessionPacer.OnSessionStarted();
            _lastHeatmapIndex = 0;
            _keyboardSound.Reset();

            // Hide between-session panels and hints during active typing
            ExplanationPanel.Hide();
            DemonstrationPanel.Hide();
            LayersPanel.Hide();
            TypingPanel.HideGuidance();

            TypingPanel.FocusTypingBox();
        }
    }

    private void ResetTest_Click(object sender, RoutedEventArgs e)
    {
        _uiFeedback.OnButtonClick();
        TypingPanel.DismissCompletionBanner();
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
        TypingPanel.DismissCompletionBanner();
        _typingEngine.CancelSession();
        _pendingContext = null; // Clear any pending intent from suggestion actions
        LoadNewSnippet();
    }

    /// <summary>
    /// Handles a suggestion being followed — loads the appropriate snippet.
    /// </summary>
    private void OnSuggestionFollowed(object? sender, PracticeSuggestion suggestion)
    {
        _uiFeedback.OnButtonClick();
        _typingEngine.CancelSession();

        switch (suggestion.Action)
        {
            case SuggestionAction.LoadWeaknessSnippet:
                LoadSnippetForWeakCharsFromPayload(suggestion.ActionPayload);
                break;

            case SuggestionAction.LoadEasySnippet:
                LoadEasySnippet();
                break;

            case SuggestionAction.LoadHarderSnippet:
                LoadHarderSnippet();
                break;

            case SuggestionAction.SwitchLanguage:
                // Switch language in settings, then load snippet for that language
                if (!string.IsNullOrEmpty(suggestion.ActionPayload))
                {
                    // Note: language switching works through SettingsPanel.SelectedLanguage
                    // which reads the combo box. We load directly for the target language.
                    LoadSnippetForLanguage(suggestion.ActionPayload);
                }
                break;

            default:
                LoadNewSnippet();
                break;
        }
    }

    /// <summary>
    /// Handles weakness practice request — loads a snippet targeting weak chars.
    /// </summary>
    private void OnWeaknessPracticeRequested(object? sender, HashSet<char> weakChars)
    {
        _uiFeedback.OnButtonClick();
        _typingEngine.CancelSession();

        var language = SettingsPanel.SelectedLanguage;
        var snippet = _smartSelector.SelectForWeakChars(language, _profile, weakChars);
        TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
        _currentSnippet = snippet;
        _pendingContext = PracticeContext.ForWeakness(string.Join(",", weakChars));
        TypingPanel.ClearTyping();
        StatsPanel.Reset();
    }

    /// <summary>
    /// Loads a snippet targeting specific weak characters from a comma-separated payload.
    /// </summary>
    private void LoadSnippetForWeakCharsFromPayload(string? payload)
    {
        var weakChars = new HashSet<char>();
        if (!string.IsNullOrEmpty(payload))
        {
            foreach (var part in payload.Split(','))
            {
                if (part.Length > 0) weakChars.Add(part[0]);
            }
        }

        if (weakChars.Count > 0)
        {
            var language = SettingsPanel.SelectedLanguage;
            var snippet = _smartSelector.SelectForWeakChars(language, _profile, weakChars);
            TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
            _currentSnippet = snippet;
            _pendingContext = PracticeContext.ForWeakness(string.Join(",", weakChars));
            TypingPanel.ClearTyping();
            StatsPanel.Reset();
        }
        else
        {
            LoadNewSnippet();
        }
    }

    /// <summary>
    /// Loads an easy snippet for warmup or trend recovery.
    /// </summary>
    private void LoadEasySnippet()
    {
        var language = SettingsPanel.SelectedLanguage;
        var allSnippets = _contentLibraryService.GetSnippets(language).ToList();
        var easy = allSnippets
            .Where(s => s.Difficulty <= 2)
            .OrderBy(_ => Random.Shared.Next())
            .FirstOrDefault();

        var snippet = easy ?? allSnippets.FirstOrDefault();
        if (snippet != null)
        {
            TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
            _currentSnippet = snippet;
            _pendingContext = new PracticeContext
            {
                Intent = PracticeIntent.Warmup,
                SystemSelected = true
            };
            TypingPanel.ClearTyping();
            StatsPanel.Reset();
        }
        else
        {
            LoadNewSnippet();
        }
    }

    /// <summary>
    /// Loads a harder snippet for exploration.
    /// </summary>
    private void LoadHarderSnippet()
    {
        var language = SettingsPanel.SelectedLanguage;
        int currentRating = _profile.GetRating(language);
        int targetDifficulty = Math.Min(5, SmartSnippetSelector.GetTargetDifficultyStatic(currentRating) + 1);

        var allSnippets = _contentLibraryService.GetSnippets(language).ToList();
        var harder = allSnippets
            .Where(s => s.Difficulty >= targetDifficulty)
            .OrderBy(_ => Random.Shared.Next())
            .FirstOrDefault();

        var snippet = harder ?? allSnippets.LastOrDefault();
        if (snippet != null)
        {
            TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
            _currentSnippet = snippet;
            _pendingContext = new PracticeContext
            {
                Intent = PracticeIntent.Exploration,
                SystemSelected = true
            };
            TypingPanel.ClearTyping();
            StatsPanel.Reset();
        }
        else
        {
            LoadNewSnippet();
        }
    }

    /// <summary>
    /// Loads a snippet for a specific language (used by "Revisit X" suggestions).
    /// </summary>
    private void LoadSnippetForLanguage(string language)
    {
        var blob = _persistenceService.Load();
        var difficultyProfile = _adaptiveDifficulty.ComputeDifficulty(
            language, _profile, blob.Longitudinal);
        var weaknessReport = _weaknessTracker.GetReport(
            language, _profile.Heatmap, blob.Longitudinal);

        var snippet = _smartSelector.SelectAdaptive(
            language, _profile, difficultyProfile, weaknessReport);

        TypingPanel.SetTarget(snippet.Title ?? "Snippet", snippet.Language, snippet.Code ?? "");
        _currentSnippet = snippet;
        _pendingContext = new PracticeContext
        {
            Intent = PracticeIntent.Exploration,
            Focus = language,
            SystemSelected = true
        };
        TypingPanel.ClearTyping();
        StatsPanel.Reset();
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
            _sessionPacer.OnSessionCompleted();

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

                // Propagate user-declared intent to the session record
                record.DeclaredIntent = e.Context?.DeclaredIntent;

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

            // Show completion banner with contextual next action
            bool isPerfect = e.ErrorCount == 0;
            var (actionLabel, actionTag) = GetCompletionAction(blob);
            TypingPanel.ShowCompletionBanner(
                e.FinalWpm, e.FinalAccuracy, e.XpEarned, isPerfect,
                actionLabel, actionTag);

            // Session retrospective — factual observations, no judgment
            var retroLines = BuildRetrospective(e, blob);
            TypingPanel.ShowRetrospective(retroLines);

            // Load next snippet
            LoadNewSnippet();
        });
    }

    /// <summary>
    /// Determines the contextual action for the completion banner.
    /// </summary>
    private (string? Label, string? Tag) GetCompletionAction(PersistedBlob blob)
    {
        var language = SettingsPanel.SelectedLanguage;

        // If there are weak characters, offer to practice them
        var weakest = blob.Profile.Heatmap.GetWeakest(count: 3, minAttempts: 5);
        if (weakest.Count > 0)
        {
            string weakChars = string.Join(",", weakest.Select(w => w.Character));
            return ("Practice weak chars", $"weakness:{weakChars}");
        }

        return (null, null);
    }

    /// <summary>
    /// Builds factual retrospective lines for the completed session.
    /// Observations only — no judgment, no "good" or "bad" framing.
    /// Returns an empty list if there's nothing meaningful to show.
    /// </summary>
    private List<string> BuildRetrospective(TypingResultEventArgs e, PersistedBlob blob)
    {
        var lines = new List<string>();
        var language = _currentSnippet?.Language ?? SettingsPanel.SelectedLanguage;

        // Show declared intent if user set one
        var declaredIntent = e.Context?.DeclaredIntent;
        if (declaredIntent.HasValue)
        {
            lines.Add($"Intent: {declaredIntent.Value}");
        }

        // Compare to recent average (last 10 sessions in this language)
        if (blob.Longitudinal.TrendsByLanguage.TryGetValue(
                language.ToLowerInvariant(), out var trend))
        {
            var avgWpm = trend.AverageWpm(10);
            var avgAcc = trend.AverageAccuracy(10);

            if (avgWpm.HasValue && trend.TotalSessions >= 3)
            {
                double wpmDelta = e.FinalWpm - avgWpm.Value;
                string wpmSign = wpmDelta >= 0 ? "+" : "";
                lines.Add($"WPM: {e.FinalWpm:F0} ({wpmSign}{wpmDelta:F0} vs recent avg)");
            }

            if (avgAcc.HasValue && trend.TotalSessions >= 3)
            {
                double accDelta = e.FinalAccuracy - avgAcc.Value;
                string accSign = accDelta >= 0 ? "+" : "";
                lines.Add($"Accuracy: {e.FinalAccuracy:F0}% ({accSign}{accDelta:F1}% vs recent avg)");
            }
        }

        // Show difficulty context
        if (_currentSnippet != null)
        {
            int rating = _profile.GetRating(language);
            int diff = _currentSnippet.Difficulty;
            if (diff > 0 && rating > 0)
            {
                lines.Add($"Difficulty {diff} \u00b7 Rating {rating}");
            }
        }

        return lines;
    }

    /// <summary>
    /// Handles clicks on the completion banner's action button.
    /// </summary>
    private void OnCompletionActionClicked(object? sender, string actionTag)
    {
        _uiFeedback.OnButtonClick();

        if (actionTag.StartsWith("weakness:"))
        {
            var payload = actionTag["weakness:".Length..];
            _typingEngine.CancelSession();
            LoadSnippetForWeakCharsFromPayload(payload);
        }
    }

    /// <summary>
    /// Handles a session note submitted by the user.
    /// Attaches the note to the most recent session record.
    /// </summary>
    private void OnSessionNoteSubmitted(object? sender, string note)
    {
        var blob = _persistenceService.Load();
        if (blob.History.Records.Count > 0)
        {
            blob.History.Records[0].Note = note.Length > 280 ? note[..280] : note;
            _persistenceService.Save(blob);
        }
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
        var language = SettingsPanel.SelectedLanguage;

        // Weakness report with trajectory context
        var weaknessReport = _weaknessTracker.GetReport(
            language, blob.Profile.Heatmap, blob.Longitudinal);
        StatsPanel.UpdateWeakSpots(blob.Profile.Heatmap, weaknessReport);

        StatsPanel.UpdateHistory(blob.History);

        // Trend analysis
        var trends = _trendAnalyzer.AnalyzeAll(blob.Longitudinal);
        StatsPanel.UpdateTrends(trends);

        // Pacing snapshot
        var pacing = _sessionPacer.GetSnapshot(blob.Longitudinal);
        StatsPanel.UpdatePacing(pacing);

        // Orientation cue — subtle context for returning users
        StatsPanel.UpdateOrientationCue(blob.History, language);

        // Typist identity — longitudinal self-portrait
        var identity = TypistIdentityService.Build(blob.History, blob.Longitudinal);
        StatsPanel.UpdateIdentity(identity);

        // Intent patterns — factual correlations, no judgment
        StatsPanel.UpdateIntentPatterns(blob.History);

        // Deeper patterns — observational, not prescriptive
        var patterns = PatternDetector.Detect(blob.History, blob.Longitudinal);
        StatsPanel.UpdatePatterns(patterns);

        // Practice suggestions
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

        // Practice preferences
        _settings.ShowIntentChips = SettingsPanel.ShowIntentChips;
        _settings.DefaultIntent = SettingsPanel.DefaultIntent;
        _settings.PracticeNote = SettingsPanel.PracticeNote;
        _settings.FocusArea = SettingsPanel.FocusArea;
        _settings.ShowSuggestions = SettingsPanel.ShowSuggestions;
        _settings.SelectedPracticeConfig = SettingsPanel.SelectedPracticeConfigName;
        _settings.ShowCommunitySignals = SettingsPanel.ShowCommunitySignals;

        // Teaching settings
        _settings.ShowScaffolds = SettingsPanel.ShowScaffolds;
        _settings.ShowDemonstrations = SettingsPanel.ShowDemonstrations;
        _settings.ShowGuidance = SettingsPanel.ShowGuidance;
        _settings.ShowSkillLayers = SettingsPanel.ShowSkillLayers;

        return _settings;
    }

    /// <summary>
    /// Applies practice preferences to the UI — shows/hides intent chips,
    /// sets default intent selection, controls suggestion visibility.
    /// </summary>
    private void ApplyPracticePreferences()
    {
        // Show or hide intent chips based on user preference
        TypingPanel.SetIntentChipsVisible(_settings.ShowIntentChips);

        // Pre-select the user's default intent, if any
        if (_settings.DefaultIntent.HasValue)
        {
            TypingPanel.SetDefaultIntent(_settings.DefaultIntent.Value);
        }

        // Relay suggestion visibility preference to StatsPanel
        StatsPanel.ShowSuggestions = _settings.ShowSuggestions;
    }

    /// <summary>
    /// Opens the user snippets folder in the system file explorer.
    /// Creates the directory if it doesn't exist yet.
    /// </summary>
    private async void OnOpenUserSnippetsFolder(object? sender, EventArgs e)
    {
        try
        {
            var path = _contentLibraryService.UserContent.EnsureUserSnippetsDirectory();
            await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }
        catch
        {
            // Silently fail — folder access might be restricted
        }
    }

    /// <summary>
    /// Opens the community content folder in the system file explorer.
    /// Creates the directory if it doesn't exist yet.
    /// </summary>
    private async void OnOpenCommunityFolder(object? sender, EventArgs e)
    {
        try
        {
            var path = _contentLibraryService.CommunityContent.EnsureCommunityContentDirectory();
            await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }
        catch
        {
            // Silently fail — folder access might be restricted
        }
    }

    /// <summary>
    /// Handles practice config selection changes.
    /// </summary>
    private void OnPracticeConfigChanged(object? sender, string configName)
    {
        _settings.SelectedPracticeConfig = configName;
        UpdateConfigDescription();
    }

    /// <summary>
    /// Updates the config description text in the settings panel.
    /// </summary>
    private void UpdateConfigDescription()
    {
        var config = _practiceConfigService.GetConfig(_settings.SelectedPracticeConfig);
        SettingsPanel.UpdateConfigDescription(config.Description);
    }

    /// <summary>
    /// Gets the currently active practice config.
    /// Used by session start to apply config overrides.
    /// </summary>
    private PracticeConfig GetActivePracticeConfig()
    {
        return _practiceConfigService.GetConfig(
            SettingsPanel.SelectedPracticeConfigName);
    }

    /// <summary>
    /// Exports user content as a portable ZIP bundle.
    /// Uses a file picker so the user chooses where to save.
    /// </summary>
    private async void OnExportBundle(object? sender, EventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("ZIP Archive", new[] { ".zip" });
            picker.SuggestedFileName = $"devop-typer-content-{DateTime.Now:yyyyMMdd}";

            // Initialize picker with window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file == null) return; // User cancelled

            var bundleService = new PortableBundleService();
            var result = bundleService.Export(file.Path, _contentLibraryService.UserContent,
                _practiceConfigService, _contentLibraryService.CommunityContent);

            SettingsPanel.ShowBundleStatus(result != null
                ? "Exported successfully"
                : "Nothing to export — no user content found");
        }
        catch (Exception ex)
        {
            SettingsPanel.ShowBundleStatus($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a portable ZIP bundle. Asks the user whether to import
    /// as personal content or community content, then routes to the
    /// appropriate directory.
    /// </summary>
    private async void OnImportBundle(object? sender, EventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".zip");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return; // User cancelled

            // Ask whether to import as personal or community content
            var dialog = new ContentDialog
            {
                Title = "Import Bundle",
                Content = "Where should this content be imported?",
                PrimaryButtonText = "My Content",
                SecondaryButtonText = "Community Content",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var choice = await dialog.ShowAsync();
            if (choice == ContentDialogResult.None) return; // Cancelled

            var bundleService = new PortableBundleService();
            BundleImportResult result;

            if (choice == ContentDialogResult.Primary)
            {
                // Import as personal content (existing v0.6.0 behavior)
                var snippetsDir = _contentLibraryService.UserContent.EnsureUserSnippetsDirectory();
                var configsDir = _practiceConfigService.EnsureUserConfigsDirectory();
                result = bundleService.Import(file.Path, snippetsDir, configsDir);
            }
            else
            {
                // Import as community content
                var communityDir = _contentLibraryService.CommunityContent.EnsureCommunityContentDirectory();
                var communitySnippetsDir = Path.Combine(communityDir, "snippets");
                var communityConfigsDir = Path.Combine(communityDir, "configs");
                result = bundleService.ImportToCommunity(file.Path, communitySnippetsDir, communityConfigsDir);
            }

            SettingsPanel.ShowBundleStatus(result.Summary);
        }
        catch (Exception ex)
        {
            SettingsPanel.ShowBundleStatus($"Import failed: {ex.Message}");
        }
    }

}
