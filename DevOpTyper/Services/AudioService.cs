using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace DevOpTyper.Services;

public sealed class AudioService
{
    private readonly MediaPlayer _ambientPlayer = new();
    private readonly List<MediaPlayer> _sfxPlayers = new();
    private readonly List<string> _ambientFiles = new();
    private readonly List<string> _keyFiles = new();

    private string _uiClick = "";

    private double _ambientVol = 0.5;
    private double _keyVol = 0.7;
    private double _uiVol = 0.6;

    public void Initialize()
    {
        // Discover files in output Assets folder
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
            var ui = Path.Combine(sfxDir, "ui_click.wav");
            if (File.Exists(ui)) _uiClick = ui;
        }

        _ambientPlayer.IsLoopingEnabled = true;

        // Pool some SFX players
        for (int i = 0; i < 6; i++)
        {
            _sfxPlayers.Add(new MediaPlayer());
        }
    }

    public void SetVolumes(double ambient, double keyboard, double ui)
    {
        _ambientVol = Clamp01(ambient);
        _keyVol = Clamp01(keyboard);
        _uiVol = Clamp01(ui);

        _ambientPlayer.Volume = _ambientVol;
    }

    public void PlayRandomAmbient()
    {
        if (_ambientFiles.Count == 0) return;
        var pick = _ambientFiles[Random.Shared.Next(_ambientFiles.Count)];
        _ambientPlayer.Source = MediaSource.CreateFromUri(new Uri(pick));
        _ambientPlayer.Volume = _ambientVol;
        _ambientPlayer.Play();
    }

    public void StopAmbient()
    {
        _ambientPlayer.Pause();
    }

    public void PlayKeyClick()
    {
        if (_keyFiles.Count == 0) return;
        var pick = _keyFiles[Random.Shared.Next(_keyFiles.Count)];
        PlayOneShot(pick, _keyVol);
    }

    public void PlayUiClick()
    {
        if (string.IsNullOrWhiteSpace(_uiClick)) return;
        PlayOneShot(_uiClick, _uiVol);
    }

    private void PlayOneShot(string filePath, double volume)
    {
        // Round-robin through pooled players
        var p = _sfxPlayers[Random.Shared.Next(_sfxPlayers.Count)];
        p.Source = MediaSource.CreateFromUri(new Uri(filePath));
        p.Volume = volume;
        p.Play();
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
}
