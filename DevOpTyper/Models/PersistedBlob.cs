namespace DevOpTyper.Models;

public sealed class PersistedBlob
{
    public Profile Profile { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
