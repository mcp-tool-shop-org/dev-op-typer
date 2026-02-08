namespace DevOpTyper.Models;

public sealed class AppSettings
{
    public string SelectedLanguage { get; set; } = "python";
    public bool IsAmbientMuted { get; set; } = false;

    public double AmbientVolume { get; set; } = 0.5;
    public double KeyboardVolume { get; set; } = 0.7;
    public double UiClickVolume { get; set; } = 0.6;

    public bool HardcoreMode { get; set; } = false;
    public bool HighContrast { get; set; } = false;

    public bool SidebarOpen { get; set; } = false;
}
