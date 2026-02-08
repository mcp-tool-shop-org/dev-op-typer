using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Input;
using DevOpTyper.Services;
using DevOpTyper.Models;

namespace DevOpTyper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PersistenceService _persistence = new();
    private readonly SnippetService _snippets = new();
    private readonly AudioService _audio = new();

    private SessionState _session = new();

    [ObservableProperty] private string selectedLanguage = "python";
    [ObservableProperty] private string currentTitle = "—";
    [ObservableProperty] private string targetText = "";
    [ObservableProperty] private string typedText = "";

    [ObservableProperty] private bool isAmbientMuted;
    [ObservableProperty] private double ambientVolume = 0.5;
    [ObservableProperty] private double keyboardVolume = 0.7;
    [ObservableProperty] private double uiClickVolume = 0.6;

    [ObservableProperty] private bool hardcoreMode;
    [ObservableProperty] private bool highContrast;

    [ObservableProperty] private double sidebarWidth = 0;

    public string LevelBadge => $"Lv {_session.Profile.Level} • {_session.Profile.Xp}xp";
    public string SessionStatus => _session.IsRunning ? "Session running" : "Ready";
    public string LiveWpmText => $"WPM: {_session.LiveWpm:0}";
    public string LiveAccuracyText => $"Accuracy: {_session.LiveAccuracy:0.0}%";
    public string ErrorsText => $"Errors: {_session.ErrorCount}";
    public string XpText => $"XP this run: {_session.XpEarned}";
    public string AmbientButtonText => IsAmbientMuted ? "Ambient: Mute" : "Ambient: Random";

    public IRelayCommand NewTestCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand ToggleSidebarCommand { get; }
    public IRelayCommand ToggleAmbientCommand { get; }
    public IRelayCommand SaveSettingsCommand { get; }

    public MainViewModel()
    {
        // Load persisted state
        var persisted = _persistence.Load();
        _session.Profile = persisted.Profile;
        ApplySettings(persisted.Settings);

        NewTestCommand = new RelayCommand(NewTest);
        ResetCommand = new RelayCommand(ResetTyping);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ToggleAmbientCommand = new RelayCommand(ToggleAmbient);
        SaveSettingsCommand = new RelayCommand(SaveSettings);

        // Start ambient if enabled
        _audio.Initialize();
        _audio.SetVolumes(AmbientVolume, KeyboardVolume, UiClickVolume);
        if (!IsAmbientMuted)
        {
            _audio.PlayRandomAmbient();
        }

        // First test
        NewTest();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        NewTest();
        SaveSettings();
    }

    partial void OnIsAmbientMutedChanged(bool value)
    {
        OnPropertyChanged(nameof(AmbientButtonText));
        if (value) _audio.StopAmbient();
        else _audio.PlayRandomAmbient();
        SaveSettings();
    }

    partial void OnAmbientVolumeChanged(double value) { _audio.SetVolumes(AmbientVolume, KeyboardVolume, UiClickVolume); SaveSettings(); }
    partial void OnKeyboardVolumeChanged(double value) { _audio.SetVolumes(AmbientVolume, KeyboardVolume, UiClickVolume); SaveSettings(); }
    partial void OnUiClickVolumeChanged(double value) { _audio.SetVolumes(AmbientVolume, KeyboardVolume, UiClickVolume); SaveSettings(); }

    private void ApplySettings(AppSettings s)
    {
        SelectedLanguage = s.SelectedLanguage;
        IsAmbientMuted = s.IsAmbientMuted;
        AmbientVolume = s.AmbientVolume;
        KeyboardVolume = s.KeyboardVolume;
        UiClickVolume = s.UiClickVolume;
        HardcoreMode = s.HardcoreMode;
        HighContrast = s.HighContrast;
        SidebarWidth = s.SidebarOpen ? 320 : 0;
    }

    private AppSettings CaptureSettings() => new()
    {
        SelectedLanguage = SelectedLanguage,
        IsAmbientMuted = IsAmbientMuted,
        AmbientVolume = AmbientVolume,
        KeyboardVolume = KeyboardVolume,
        UiClickVolume = UiClickVolume,
        HardcoreMode = HardcoreMode,
        HighContrast = HighContrast,
        SidebarOpen = SidebarWidth > 0
    };

    private void NewTest()
    {
        var snip = _snippets.GetSnippet(SelectedLanguage, _session.Profile.RatingByLanguage);
        CurrentTitle = snip.Title;
        TargetText = snip.Code;
        TypedText = "";

        _session.Start(TargetText);
        OnPropertyChanged(nameof(SessionStatus));
    }

    private void ResetTyping()
    {
        TypedText = "";
        _session.Start(TargetText);
        OnPropertyChanged(nameof(SessionStatus));
    }

    private void ToggleSidebar()
    {
        _audio.PlayUiClick();
        SidebarWidth = SidebarWidth > 0 ? 0 : 320;
        SaveSettings();
    }

    private void ToggleAmbient()
    {
        _audio.PlayUiClick();
        IsAmbientMuted = !IsAmbientMuted;
    }

    private void SaveSettings()
    {
        var blob = new PersistedBlob
        {
            Profile = _session.Profile,
            Settings = CaptureSettings()
        };
        _persistence.Save(blob);
        OnPropertyChanged(nameof(LevelBadge));
    }

    public void OnKeyDown(KeyRoutedEventArgs e)
    {
        // Keyboard SFX (skip modifier-only presses)
        if (e.Key is not Windows.System.VirtualKey.Shift
            and not Windows.System.VirtualKey.Control
            and not Windows.System.VirtualKey.Menu
            and not Windows.System.VirtualKey.LeftWindows
            and not Windows.System.VirtualKey.RightWindows)
        {
            _audio.PlayKeyClick();
        }

        _session.Update(TypedText, HardcoreMode);

        // Completion check
        if (_session.IsComplete)
        {
            // Award XP
            _session.Profile.AddXp(_session.XpEarned);
            SaveSettings();

            // Next
            NewTest();
        }

        OnPropertyChanged(nameof(LiveWpmText));
        OnPropertyChanged(nameof(LiveAccuracyText));
        OnPropertyChanged(nameof(ErrorsText));
        OnPropertyChanged(nameof(XpText));
        OnPropertyChanged(nameof(LevelBadge));
    }
}
