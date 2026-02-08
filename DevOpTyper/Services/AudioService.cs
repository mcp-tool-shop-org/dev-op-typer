using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DevOpTyper.Services;

/// <summary>
/// Audio service for unpackaged WinUI 3 apps.
/// - SFX: NAudio WASAPI with pre-loaded buffers, polyphony, and pitch variation
///   (mirrors lokey-typer's Web Audio API pattern: createBufferSource + polyphony cap)
/// - Ambient: mciSendString with mpegvideo for looping background playback
/// </summary>
public sealed class AudioService
{
    #region Win32 Interop (ambient only)

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, StringBuilder? returnString, int returnSize, IntPtr callback);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciGetErrorString(int errorCode, StringBuilder errorText, int errorLength);

    #endregion

    #region SFX Engine (NAudio WASAPI — like Web Audio API)

    // Pre-decoded audio buffers keyed by file path
    private readonly Dictionary<string, byte[]> _sfxBuffers = new();
    private readonly Dictionary<string, WaveFormat> _sfxFormats = new();

    // Shared WASAPI mixer for polyphonic SFX playback
    private WasapiOut? _sfxOutput;
    private MixingSampleProvider? _sfxMixer;

    private void InitSfxEngine()
    {
        // Create a mixer at 44100Hz stereo (standard output format)
        var mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        _sfxMixer = new MixingSampleProvider(mixerFormat)
        {
            ReadFully = true // keep playing silence when no inputs (keeps device open)
        };

        _sfxOutput = new WasapiOut(
            NAudio.CoreAudioApi.AudioClientShareMode.Shared,
            50 // latency in ms
        );
        _sfxOutput.Init(_sfxMixer);
        _sfxOutput.Play();

        Log("SFX engine: WASAPI mixer started (44100Hz stereo, 50ms latency)");
    }

    private void PreloadSfx(string filePath)
    {
        try
        {
            using var reader = new AudioFileReader(filePath);
            // Read entire file into memory as float samples
            var samples = new float[(int)(reader.Length / sizeof(float)) + 1024];
            int read = reader.Read(samples, 0, samples.Length);
            Array.Resize(ref samples, read);

            // Store raw PCM bytes and format
            var bytes = new byte[read * sizeof(float)];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            _sfxBuffers[filePath] = bytes;
            _sfxFormats[filePath] = reader.WaveFormat;
        }
        catch (Exception ex)
        {
            Log($"PreloadSfx FAILED: {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    private void PlaySfxBuffered(string filePath, float volume)
    {
        if (_sfxMixer == null || !_sfxBuffers.ContainsKey(filePath)) return;

        var bytes = _sfxBuffers[filePath];
        var format = _sfxFormats[filePath];

        // Copy buffer (each playback needs its own copy)
        var sampleCount = bytes.Length / sizeof(float);
        var samples = new float[sampleCount];
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);

        // Apply volume
        for (int i = 0; i < samples.Length; i++)
            samples[i] *= volume;

        // Create a sample provider from the buffer
        var bufferProvider = new BufferSampleProvider(samples, format);

        // Add pitch variation ±3% (like lokey-typer: 0.98 + Math.random() * 0.06)
        // NAudio doesn't have native pitch shift on raw buffers, so we skip for now
        // and rely on the 8 different samples for variation

        try
        {
            // Convert to mixer format if needed (mono→stereo, resample)
            ISampleProvider source = bufferProvider;
            if (format.Channels == 1)
                source = new MonoToStereoSampleProvider(source);

            _sfxMixer.AddMixerInput(source);
        }
        catch (Exception ex)
        {
            Log($"PlaySfxBuffered error: {ex.Message}");
        }
    }

    #endregion

    private readonly List<string> _ambientFiles = new();
    private List<string> _keyFiles = new();
    private readonly List<string> _errorFiles = new();
    private readonly List<string> _successFiles = new();
    private string _uiClick = "";

    // Theme support
    private readonly Dictionary<string, List<string>> _themeKeyFiles = new();
    private string _currentTheme = "Mechanical";
    private string _sfxDir = "";

    // Soundscape support
    private readonly Dictionary<string, List<string>> _soundscapeFiles = new();
    private string _currentSoundscape = "Default";

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

    // Public theme info
    public string CurrentTheme => _currentTheme;
    public IReadOnlyList<string> AvailableThemes => _themeKeyFiles.Keys.OrderBy(k => k).ToList();

    // Public soundscape info
    public string CurrentSoundscape => _currentSoundscape;
    public IReadOnlyList<string> AvailableSoundscapes => _soundscapeFiles.Keys.OrderBy(k => k).ToList();

    public void Initialize()
    {
        if (_isInitialized) return;

        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
        var ambDir = Path.Combine(baseDir, "Ambient");
        _sfxDir = Path.Combine(baseDir, "Sfx");

        if (Directory.Exists(ambDir))
        {
            // Discover soundscapes from subdirectories
            foreach (var scapeDir in Directory.GetDirectories(ambDir))
            {
                var scapeName = Path.GetFileName(scapeDir);
                var wavFiles = Directory.GetFiles(scapeDir, "*.wav").OrderBy(x => x).ToList();
                if (wavFiles.Count > 0)
                {
                    _soundscapeFiles[scapeName] = wavFiles;
                    Log($"Init: Soundscape '{scapeName}' = {wavFiles.Count} ambient tracks");
                }
            }

            // Set default soundscape
            if (_soundscapeFiles.ContainsKey(_currentSoundscape))
                _ambientFiles.AddRange(_soundscapeFiles[_currentSoundscape]);
            else if (_soundscapeFiles.Count > 0)
            {
                _currentSoundscape = _soundscapeFiles.Keys.First();
                _ambientFiles.AddRange(_soundscapeFiles[_currentSoundscape]);
            }

            // Backward compat: if no subdirectories found, check for loose WAV files
            if (_soundscapeFiles.Count == 0)
            {
                var looseFiles = Directory.GetFiles(ambDir, "*.wav").OrderBy(x => x).ToList();
                if (looseFiles.Count > 0)
                {
                    _soundscapeFiles["Default"] = looseFiles;
                    _ambientFiles.AddRange(looseFiles);
                    Log($"Init: Fallback - found {looseFiles.Count} loose ambient files in root");
                }
            }
        }

        if (Directory.Exists(_sfxDir))
        {
            // Discover keyboard themes from subdirectories
            foreach (var themeDir in Directory.GetDirectories(_sfxDir))
            {
                var themeName = Path.GetFileName(themeDir);
                var keyFiles = Directory.GetFiles(themeDir, "key_*.wav").OrderBy(x => x).ToList();
                if (keyFiles.Count > 0)
                {
                    _themeKeyFiles[themeName] = keyFiles;
                    Log($"Init: Theme '{themeName}' = {keyFiles.Count} key sounds");
                }
            }

            // Set default theme
            if (_themeKeyFiles.ContainsKey(_currentTheme))
                _keyFiles = _themeKeyFiles[_currentTheme];
            else if (_themeKeyFiles.Count > 0)
            {
                _currentTheme = _themeKeyFiles.Keys.First();
                _keyFiles = _themeKeyFiles[_currentTheme];
            }

            // Non-theme SFX (error, success, ui_click stay in root Sfx dir)
            _errorFiles.AddRange(Directory.GetFiles(_sfxDir, "error_*.wav").OrderBy(x => x));
            _successFiles.AddRange(Directory.GetFiles(_sfxDir, "success_*.wav").OrderBy(x => x));

            var ui = Path.Combine(_sfxDir, "ui_click.wav");
            if (File.Exists(ui)) _uiClick = ui;
        }

        Log($"Init: BaseDir={baseDir}");
        Log($"Init: Ambient={_ambientFiles.Count}, Keys={_keyFiles.Count}, Errors={_errorFiles.Count}, Success={_successFiles.Count}, UI={HasUiClick}");

        // Initialize NAudio WASAPI mixer for SFX
        try
        {
            InitSfxEngine();

            // Pre-load ALL theme SFX into memory buffers (like Web Audio decodeAudioData)
            foreach (var themeFiles in _themeKeyFiles.Values)
                foreach (var f in themeFiles) PreloadSfx(f);
            foreach (var f in _errorFiles) PreloadSfx(f);
            foreach (var f in _successFiles) PreloadSfx(f);
            if (!string.IsNullOrEmpty(_uiClick)) PreloadSfx(_uiClick);

            Log($"Init: Pre-loaded {_sfxBuffers.Count} SFX buffers into memory");
        }
        catch (Exception ex)
        {
            Log($"Init: SFX engine FAILED: {ex.Message}");
        }

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

    #region Theme Switching

    public void SwitchKeyboardTheme(string theme)
    {
        if (!_themeKeyFiles.ContainsKey(theme))
        {
            Log($"SwitchKeyboardTheme: unknown theme '{theme}', available: {string.Join(", ", _themeKeyFiles.Keys)}");
            return;
        }

        _currentTheme = theme;
        _keyFiles = _themeKeyFiles[theme];
        Log($"SwitchKeyboardTheme: switched to '{theme}' ({_keyFiles.Count} key sounds)");
    }

    public void SwitchSoundscape(string name)
    {
        if (!_soundscapeFiles.ContainsKey(name))
        {
            Log($"SwitchSoundscape: unknown soundscape '{name}', available: {string.Join(", ", _soundscapeFiles.Keys)}");
            return;
        }

        _currentSoundscape = name;
        _ambientFiles.Clear();
        _ambientFiles.AddRange(_soundscapeFiles[name]);
        _currentAmbientIndex = -1;
        Log($"SwitchSoundscape: switched to '{name}' ({_ambientFiles.Count} tracks)");

        // If ambient was playing, restart with first track of new soundscape
        if (_isAmbientPlaying)
        {
            PlayAmbientTrack(0);
        }
    }

    #endregion

    #region Ambient Playback (mciSendString with mpegvideo)

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
        Log($"PlayAmbientFile: {filePath}");

        MciCmd($"stop {_currentAmbientAlias}");
        MciCmd($"close {_currentAmbientAlias}");

        var openResult = MciCmd($"open \"{filePath}\" type mpegvideo alias {_currentAmbientAlias}");
        Log($"  mci open result: {openResult} ({GetMciError(openResult)})");
        if (openResult != 0)
        {
            openResult = MciCmd($"open \"{filePath}\" alias {_currentAmbientAlias}");
            if (openResult != 0) return;
        }

        ApplyAmbientVolume();

        var playResult = MciCmd($"play {_currentAmbientAlias} repeat");
        if (playResult != 0)
            playResult = MciCmd($"play {_currentAmbientAlias}");

        _isAmbientPlaying = playResult == 0;
    }

    private void ApplyAmbientVolume()
    {
        int vol = (_masterMuted || _ambientMuted) ? 0 : (int)(_ambientVol * 1000);
        MciCmd($"setaudio {_currentAmbientAlias} volume to {vol}");
    }

    /// <summary>
    /// Replays the current ambient track from the start (or first track if none selected).
    /// Use this when unmuting — keeps the same track instead of randomizing.
    /// </summary>
    public void ResumeCurrentAmbient()
    {
        if (_ambientFiles.Count == 0) return;
        int idx = _currentAmbientIndex >= 0 ? _currentAmbientIndex : 0;
        PlayAmbientTrack(idx);
    }

    public void StopAmbient()
    {
        MciCmd($"stop {_currentAmbientAlias}");
        MciCmd($"close {_currentAmbientAlias}");
        _isAmbientPlaying = false;
    }

    public void PauseAmbient()
    {
        MciCmd($"pause {_currentAmbientAlias}");
    }

    public void ResumeAmbient()
    {
        if (_isAmbientPlaying)
            MciCmd($"resume {_currentAmbientAlias}");
    }

    private int MciCmd(string command)
    {
        return mciSendString(command, null, 0, IntPtr.Zero);
    }

    private static string GetMciError(int errorCode)
    {
        if (errorCode == 0) return "OK";
        var sb = new StringBuilder(256);
        mciGetErrorString(errorCode, sb, sb.Capacity);
        return sb.ToString();
    }

    #endregion

    #region SFX Playback (NAudio buffered — polyphonic, instant)

    public void PlayKeyClick()
    {
        if (_keyFiles.Count == 0 || _keyMuted || _masterMuted) return;
        var pick = _keyFiles[Random.Shared.Next(_keyFiles.Count)];
        PlaySfxBuffered(pick, (float)_keyVol);
    }

    public void PlayUiClick()
    {
        if (string.IsNullOrWhiteSpace(_uiClick) || _uiMuted || _masterMuted) return;
        PlaySfxBuffered(_uiClick, (float)_uiVol);
    }

    public void PlayError()
    {
        if (_errorFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _errorFiles[Random.Shared.Next(_errorFiles.Count)];
        PlaySfxBuffered(pick, (float)_uiVol * 0.6f); // 60% volume like lokey-typer error sounds
    }

    public void PlaySuccess()
    {
        if (_successFiles.Count == 0 || _uiMuted || _masterMuted) return;
        var pick = _successFiles[Random.Shared.Next(_successFiles.Count)];
        PlaySfxBuffered(pick, (float)_uiVol);
    }

    #endregion

    #region Helpers

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "audio_debug.log");

    private static void Log(string msg)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n";
            File.AppendAllText(_logPath, line);
            System.Diagnostics.Debug.WriteLine($"[AudioService] {msg}");
        }
        catch { }
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        StopAmbient();
        _sfxOutput?.Stop();
        _sfxOutput?.Dispose();
    }

    #endregion
}

/// <summary>
/// Simple ISampleProvider that plays a pre-decoded float buffer once, then signals completion.
/// Equivalent to Web Audio API's AudioBufferSourceNode.
/// </summary>
internal sealed class BufferSampleProvider : ISampleProvider
{
    private readonly float[] _buffer;
    private int _position;

    public WaveFormat WaveFormat { get; }

    public BufferSampleProvider(float[] buffer, WaveFormat format)
    {
        _buffer = buffer;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var remaining = _buffer.Length - _position;
        if (remaining <= 0) return 0;

        var toCopy = Math.Min(count, remaining);
        Array.Copy(_buffer, _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }
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
