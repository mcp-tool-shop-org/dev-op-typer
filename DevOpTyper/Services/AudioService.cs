using System.Runtime.InteropServices;

namespace DevOpTyper.Services;

/// <summary>
/// Audio service using Win32 native APIs for reliable playback in unpackaged WinUI 3 apps.
/// - SFX: uses PlaySound (fire-and-forget, async)
/// - Ambient: uses mciSendString for looping background playback with volume control
/// </summary>
public sealed class AudioService
{
    #region Win32 Interop

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, System.Text.StringBuilder? returnString, int returnSize, IntPtr callback);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    // PlaySound flags
    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;

    #endregion

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
    private string _currentAmbientAlias = "dotAmbient";
    private int _currentAmbientIndex = -1;

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

        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
        var ambDir = Path.Combine(baseDir, "Ambient");
        var sfxDir = Path.Combine(baseDir, "Sfx");

        if (Directory.Exists(ambDir))
        {
            _ambientFiles.AddRange(Directory.GetFiles(ambDir, "*.wav").OrderBy(x => x));
        }

        if (Directory.Exists(sfxDir))
        {
            _keyFiles.AddRange(Directory.GetFiles(sfxDir, "key_*.wav").OrderBy(x => x));
            _errorFiles.AddRange(Directory.GetFiles(sfxDir, "error_*.wav").OrderBy(x => x));
            _successFiles.AddRange(Directory.GetFiles(sfxDir, "success_*.wav").OrderBy(x => x));

            var ui = Path.Combine(sfxDir, "ui_click.wav");
            if (File.Exists(ui)) _uiClick = ui;
        }

        System.Diagnostics.Debug.WriteLine($"[AudioService] Init: Ambient={_ambientFiles.Count}, Keys={_keyFiles.Count}, UI={HasUiClick}");

        _isInitialized = true;
    }

    #region Volume Control

    public void SetAmbientVolume(double volume)
    {
        var oldVol = _ambientVol;
        _ambientVol = Clamp01(volume);
        ApplyAmbientVolume();
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
        ApplyAmbientVolume();
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
        ApplyAmbientVolume();
        MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(AudioChannel.Master, muted));
    }

    public void ToggleAmbientMute() => SetAmbientMuted(!_ambientMuted);
    public void ToggleKeyboardMute() => SetKeyboardMuted(!_keyMuted);
    public void ToggleUiMute() => SetUiMuted(!_uiMuted);
    public void ToggleMasterMute() => SetMasterMuted(!_masterMuted);

    #endregion

    #region Ambient Playback (mciSendString)

    public void PlayRandomAmbient()
    {
        if (_ambientFiles.Count == 0) return;

        int idx;
        do { idx = Random.Shared.Next(_ambientFiles.Count); }
        while (idx == _currentAmbientIndex && _ambientFiles.Count > 1);

        _currentAmbientIndex = idx;
        PlayAmbientFile(_ambientFiles[idx]);
    }

    public void PlayAmbientTrack(int index)
    {
        if (index < 0 || index >= _ambientFiles.Count) return;
        _currentAmbientIndex = index;
        PlayAmbientFile(_ambientFiles[index]);
    }

    private void PlayAmbientFile(string filePath)
    {
        // Stop any current ambient
        MciCommand($"close {_currentAmbientAlias}");

        // Open and play with repeat
        var result = MciCommand($"open \"{filePath}\" type waveaudio alias {_currentAmbientAlias}");
        if (result != 0)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] mci open failed ({result}): {filePath}");
            return;
        }

        ApplyAmbientVolume();
        MciCommand($"play {_currentAmbientAlias} repeat");
        _isAmbientPlaying = true;
        System.Diagnostics.Debug.WriteLine($"[AudioService] Ambient playing: {Path.GetFileName(filePath)}");
    }

    private void ApplyAmbientVolume()
    {
        int vol = (_masterMuted || _ambientMuted) ? 0 : (int)(_ambientVol * 1000);
        MciCommand($"setaudio {_currentAmbientAlias} volume to {vol}");
    }

    public void StopAmbient()
    {
        MciCommand($"stop {_currentAmbientAlias}");
        MciCommand($"close {_currentAmbientAlias}");
        _isAmbientPlaying = false;
    }

    public void PauseAmbient()
    {
        MciCommand($"pause {_currentAmbientAlias}");
    }

    public void ResumeAmbient()
    {
        if (_isAmbientPlaying)
        {
            MciCommand($"resume {_currentAmbientAlias}");
        }
    }

    private int MciCommand(string command)
    {
        return mciSendString(command, null, 0, IntPtr.Zero);
    }

    #endregion

    #region SFX Playback (PlaySound - fire and forget)

    public void PlayKeyClick()
    {
        if (_keyFiles.Count == 0 || _keyMuted || _masterMuted) return;
        var pick = _keyFiles[Random.Shared.Next(_keyFiles.Count)];
        PlaySfx(pick);
    }

    public void PlayUiClick()
    {
        if (string.IsNullOrWhiteSpace(_uiClick) || _uiMuted || _masterMuted) return;
        PlaySfx(_uiClick);
    }

    public void PlayError()
    {
        if (_errorFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _errorFiles[Random.Shared.Next(_errorFiles.Count)];
        PlaySfx(pick);
    }

    public void PlaySuccess()
    {
        if (_successFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _successFiles[Random.Shared.Next(_successFiles.Count)];
        PlaySfx(pick);
    }

    private void PlaySfx(string filePath)
    {
        // SND_ASYNC returns immediately, playback happens on the calling thread
        // Do NOT use Task.Run — thread pool threads have no message pump for async playback
        PlaySound(filePath, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT);
    }

    #endregion

    #region Helpers

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    #endregion

    #region Cleanup

    public void Dispose()
    {
        StopAmbient();
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
