using Windows.Media.Core;
using Windows.Media.Playback;

namespace DevOpTyper.Services;

/// <summary>
/// Audio service with volume controls, mute toggles, and channel management.
/// </summary>
public sealed class AudioService
{
    private readonly MediaPlayer _ambientPlayer = new();
    private readonly List<MediaPlayer> _sfxPlayers = new();
    private readonly List<string> _ambientFiles = new();
    private readonly List<string> _keyFiles = new();
    private readonly List<string> _errorFiles = new();
    private readonly List<string> _successFiles = new();

    private string _uiClick = "";

    // Volume levels (0.0 - 1.0)
    private double _ambientVol = 0.5;
    private double _keyVol = 0.7;
    private double _uiVol = 0.6;

    // Mute toggles
    private bool _ambientMuted;
    private bool _keyMuted;
    private bool _uiMuted;
    private bool _masterMuted;

    // State tracking
    private bool _isInitialized;
    private bool _isAmbientPlaying;
    private int _sfxPlayerIndex;

    // Events
    public event EventHandler<AudioVolumeChangedEventArgs>? VolumeChanged;
    public event EventHandler<AudioMuteChangedEventArgs>? MuteChanged;

    // Public properties
    public double AmbientVolume => _ambientVol;
    public double KeyboardVolume => _keyVol;
    public double UiVolume => _uiVol;
    public bool IsAmbientMuted => _ambientMuted;
    public bool IsKeyboardMuted => _keyMuted;
    public bool IsUiMuted => _uiMuted;
    public bool IsMasterMuted => _masterMuted;
    public bool IsInitialized => _isInitialized;
    public bool IsAmbientPlaying => _isAmbientPlaying;
    public int AmbientTrackCount => _ambientFiles.Count;
    public int KeySoundCount => _keyFiles.Count;
    public bool HasUiClick => !string.IsNullOrEmpty(_uiClick);
    public bool HasErrorSounds => _errorFiles.Count > 0;
    public bool HasSuccessSounds => _successFiles.Count > 0;

    public void Initialize()
    {
        if (_isInitialized) return;

        // Discover files in output Assets folder
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");

        var ambDir = Path.Combine(baseDir, "Ambient");
        var sfxDir = Path.Combine(baseDir, "Sfx");

        if (Directory.Exists(ambDir))
        {
            _ambientFiles.AddRange(Directory.GetFiles(ambDir, "*.wav").OrderBy(x => x));
            _ambientFiles.AddRange(Directory.GetFiles(ambDir, "*.mp3").OrderBy(x => x));
        }

        if (Directory.Exists(sfxDir))
        {
            _keyFiles.AddRange(Directory.GetFiles(sfxDir, "key_*.wav").OrderBy(x => x));
            _errorFiles.AddRange(Directory.GetFiles(sfxDir, "error_*.wav").OrderBy(x => x));
            _successFiles.AddRange(Directory.GetFiles(sfxDir, "success_*.wav").OrderBy(x => x));

            var ui = Path.Combine(sfxDir, "ui_click.wav");
            if (File.Exists(ui)) _uiClick = ui;
        }

        _ambientPlayer.IsLoopingEnabled = true;
        _ambientPlayer.MediaEnded += OnAmbientEnded;

        // Pool SFX players for concurrent sounds
        for (int i = 0; i < 8; i++)
        {
            _sfxPlayers.Add(new MediaPlayer());
        }

        _isInitialized = true;
    }

    private void OnAmbientEnded(MediaPlayer sender, object args)
    {
        // Auto-advance to next ambient track
        if (_isAmbientPlaying && _ambientFiles.Count > 1)
        {
            PlayRandomAmbient();
        }
    }

    #region Volume Control

    public void SetAmbientVolume(double volume)
    {
        var oldVol = _ambientVol;
        _ambientVol = Clamp01(volume);
        _ambientPlayer.Volume = GetEffectiveVolume(_ambientVol, _ambientMuted);
        VolumeChanged?.Invoke(this, new AudioVolumeChangedEventArgs(AudioChannel.Ambient, oldVol, _ambientVol));
    }

    public void SetKeyboardVolume(double volume)
    {
        var oldVol = _keyVol;
        _keyVol = Clamp01(volume);
        VolumeChanged?.Invoke(this, new AudioVolumeChangedEventArgs(AudioChannel.Keyboard, oldVol, _keyVol));
    }

    public void SetUiVolume(double volume)
    {
        var oldVol = _uiVol;
        _uiVol = Clamp01(volume);
        VolumeChanged?.Invoke(this, new AudioVolumeChangedEventArgs(AudioChannel.Ui, oldVol, _uiVol));
    }

    public void SetVolumes(double ambient, double keyboard, double ui)
    {
        SetAmbientVolume(ambient);
        SetKeyboardVolume(keyboard);
        SetUiVolume(ui);
    }

    #endregion

    #region Mute Control

    public void SetAmbientMuted(bool muted)
    {
        if (_ambientMuted == muted) return;
        _ambientMuted = muted;
        _ambientPlayer.Volume = GetEffectiveVolume(_ambientVol, _ambientMuted);
        MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(AudioChannel.Ambient, muted));
    }

    public void SetKeyboardMuted(bool muted)
    {
        if (_keyMuted == muted) return;
        _keyMuted = muted;
        MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(AudioChannel.Keyboard, muted));
    }

    public void SetUiMuted(bool muted)
    {
        if (_uiMuted == muted) return;
        _uiMuted = muted;
        MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(AudioChannel.Ui, muted));
    }

    public void SetMasterMuted(bool muted)
    {
        if (_masterMuted == muted) return;
        _masterMuted = muted;
        _ambientPlayer.Volume = GetEffectiveVolume(_ambientVol, _ambientMuted);
        MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(AudioChannel.Master, muted));
    }

    public void ToggleAmbientMute() => SetAmbientMuted(!_ambientMuted);
    public void ToggleKeyboardMute() => SetKeyboardMuted(!_keyMuted);
    public void ToggleUiMute() => SetUiMuted(!_uiMuted);
    public void ToggleMasterMute() => SetMasterMuted(!_masterMuted);

    #endregion

    #region Playback

    public void PlayRandomAmbient()
    {
        if (_ambientFiles.Count == 0) return;

        var pick = _ambientFiles[Random.Shared.Next(_ambientFiles.Count)];
        _ambientPlayer.Source = MediaSource.CreateFromUri(new Uri(pick));
        _ambientPlayer.Volume = GetEffectiveVolume(_ambientVol, _ambientMuted);
        _ambientPlayer.Play();
        _isAmbientPlaying = true;
    }

    public void PlayAmbientTrack(int index)
    {
        if (index < 0 || index >= _ambientFiles.Count) return;

        _ambientPlayer.Source = MediaSource.CreateFromUri(new Uri(_ambientFiles[index]));
        _ambientPlayer.Volume = GetEffectiveVolume(_ambientVol, _ambientMuted);
        _ambientPlayer.Play();
        _isAmbientPlaying = true;
    }

    public void StopAmbient()
    {
        _ambientPlayer.Pause();
        _isAmbientPlaying = false;
    }

    public void PauseAmbient()
    {
        _ambientPlayer.Pause();
    }

    public void ResumeAmbient()
    {
        if (_isAmbientPlaying)
        {
            _ambientPlayer.Play();
        }
    }

    public void PlayKeyClick()
    {
        if (_keyFiles.Count == 0 || _keyMuted || _masterMuted) return;
        var pick = _keyFiles[Random.Shared.Next(_keyFiles.Count)];
        PlayOneShot(pick, _keyVol);
    }

    public void PlayUiClick()
    {
        if (string.IsNullOrWhiteSpace(_uiClick) || _uiMuted || _masterMuted) return;
        PlayOneShot(_uiClick, _uiVol);
    }

    public void PlayError()
    {
        if (_errorFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _errorFiles[Random.Shared.Next(_errorFiles.Count)];
        PlayOneShot(pick, _uiVol);
    }

    public void PlaySuccess()
    {
        if (_successFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _successFiles[Random.Shared.Next(_successFiles.Count)];
        PlayOneShot(pick, _uiVol);
    }

    private void PlayOneShot(string filePath, double volume)
    {
        // Round-robin through pooled players
        _sfxPlayerIndex = (_sfxPlayerIndex + 1) % _sfxPlayers.Count;
        var p = _sfxPlayers[_sfxPlayerIndex];
        p.Source = MediaSource.CreateFromUri(new Uri(filePath));
        p.Volume = GetEffectiveVolume(volume, false);
        p.Play();
    }

    #endregion

    #region Helpers

    private double GetEffectiveVolume(double baseVolume, bool isMuted)
    {
        if (_masterMuted || isMuted) return 0;
        return baseVolume;
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _ambientPlayer.MediaEnded -= OnAmbientEnded;
        _ambientPlayer.Dispose();
        foreach (var p in _sfxPlayers)
        {
            p.Dispose();
        }
        _sfxPlayers.Clear();
    }

    #endregion
}

#region Event Args

public enum AudioChannel
{
    Master,
    Ambient,
    Keyboard,
    Ui
}

public class AudioVolumeChangedEventArgs : EventArgs
{
    public AudioChannel Channel { get; }
    public double OldVolume { get; }
    public double NewVolume { get; }

    public AudioVolumeChangedEventArgs(AudioChannel channel, double oldVolume, double newVolume)
    {
        Channel = channel;
        OldVolume = oldVolume;
        NewVolume = newVolume;
    }
}

public class AudioMuteChangedEventArgs : EventArgs
{
    public AudioChannel Channel { get; }
    public bool IsMuted { get; }

    public AudioMuteChangedEventArgs(AudioChannel channel, bool isMuted)
    {
        Channel = channel;
        IsMuted = isMuted;
    }
}

#endregion
