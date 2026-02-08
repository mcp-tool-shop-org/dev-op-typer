namespace DevOpTyper.Models;

/// <summary>
/// Root object for all persisted application data.
/// </summary>
public sealed class PersistedBlob
{
    /// <summary>
    /// User profile with level, XP, and ratings.
    /// </summary>
    public Profile Profile { get; set; } = new();

    /// <summary>
    /// Application settings.
    /// </summary>
    public AppSettings Settings { get; set; } = new();

    /// <summary>
    /// Session history for statistics tracking.
    /// </summary>
    public SessionHistory History { get; set; } = new();

    /// <summary>
    /// Cross-session longitudinal data for trend awareness (v0.3.0+).
    /// Null for data created before v0.3.0 — deserialized as new().
    /// </summary>
    public LongitudinalData Longitudinal { get; set; } = new();

    /// <summary>
    /// IDs of favorited snippets.
    /// </summary>
    public HashSet<string> FavoriteSnippetIds { get; set; } = new();

    /// <summary>
    /// Last practiced date per language.
    /// </summary>
    public Dictionary<string, DateTime> LastPracticedByLanguage { get; set; } = new();

    /// <summary>
    /// Timestamp of last sync (for future cloud sync).
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}
