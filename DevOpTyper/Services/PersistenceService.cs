using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Service for persisting app data with versioning and backup.
/// </summary>
public sealed class PersistenceService
{
    private const string Key = "DevOpTyper.PersistedBlob.v1";
    private const string BackupKey = "DevOpTyper.PersistedBlob.backup";
    private const string VersionKey = "DevOpTyper.SchemaVersion";
    private const int CurrentSchemaVersion = 3;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private PersistedBlob? _cachedBlob;
    private bool _isDirty;

    // Events
    public event EventHandler<DataLoadedEventArgs>? DataLoaded;
    public event EventHandler<DataSavedEventArgs>? DataSaved;

    /// <summary>
    /// Gets whether there are unsaved changes.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Load persisted data with migration support.
    /// </summary>
    public PersistedBlob Load()
    {
        if (_cachedBlob != null) return _cachedBlob;

        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            // Check and migrate schema version
            var version = GetSchemaVersion(settings);
            if (version < CurrentSchemaVersion)
            {
                MigrateSchema(settings, version);
            }

            if (settings.Values.TryGetValue(Key, out var obj) && obj is string json && !string.IsNullOrWhiteSpace(json))
            {
                var blob = JsonSerializer.Deserialize<PersistedBlob>(json, SerializerOptions);
                if (blob is not null)
                {
                    SanitizeBlob(blob);
                    _cachedBlob = blob;
                    DataLoaded?.Invoke(this, new DataLoadedEventArgs(true, _cachedBlob));
                    return blob;
                }
            }
        }
        catch (Exception ex)
        {
            // Try loading from backup
            try
            {
                var backup = TryLoadBackup();
                if (backup != null)
                {
                    SanitizeBlob(backup);
                    _cachedBlob = backup;
                    DataLoaded?.Invoke(this, new DataLoadedEventArgs(true, _cachedBlob, fromBackup: true));
                    return backup;
                }
            }
            catch
            {
                // Backup also corrupt — fall through to fresh blob
            }

            DataLoaded?.Invoke(this, new DataLoadedEventArgs(false, null, error: ex.Message));
        }

        _cachedBlob = new PersistedBlob();
        return _cachedBlob;
    }

    /// <summary>
    /// Validates and clamps deserialized data to prevent nonsense values
    /// from corrupt or hand-edited state files.
    /// </summary>
    private static void SanitizeBlob(PersistedBlob blob)
    {
        // Profile
        blob.Profile ??= new();
        if (blob.Profile.Xp < 0) blob.Profile.Xp = 0;
        if (blob.Profile.Level < 1) blob.Profile.Level = 1;
        blob.Profile.Heatmap ??= new();
        blob.Profile.WeakChars ??= new();

        // Settings
        blob.Settings ??= new();
        blob.Settings.TypingRules ??= new();
        blob.Settings.AmbientVolume = Math.Clamp(blob.Settings.AmbientVolume, 0, 1);
        blob.Settings.KeyboardVolume = Math.Clamp(blob.Settings.KeyboardVolume, 0, 1);
        blob.Settings.UiClickVolume = Math.Clamp(blob.Settings.UiClickVolume, 0, 1);
        blob.Settings.TypingRules.AccuracyFloorForXp = Math.Clamp(blob.Settings.TypingRules.AccuracyFloorForXp, 0, 100);

        // Practice preferences (v0.4.0) — clamp note length, validate enum range
        if (blob.Settings.PracticeNote?.Length > 200)
            blob.Settings.PracticeNote = blob.Settings.PracticeNote[..200];
        if (blob.Settings.DefaultIntent.HasValue &&
            !Enum.IsDefined(typeof(UserIntent), blob.Settings.DefaultIntent.Value))
            blob.Settings.DefaultIntent = null;

        // History
        blob.History ??= new();
        blob.History.Records ??= new();

        // Focus area validation (v0.4.0) — must be one of the known values or null
        var validFocusAreas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "brackets", "operators", "strings", "control", "functions", "data" };
        if (!string.IsNullOrEmpty(blob.Settings.FocusArea) &&
            !validFocusAreas.Contains(blob.Settings.FocusArea))
            blob.Settings.FocusArea = null;

        // Sanitize session records — clamp impossible values
        foreach (var r in blob.History.Records)
        {
            if (double.IsNaN(r.Wpm) || double.IsInfinity(r.Wpm) || r.Wpm < 0) r.Wpm = 0;
            if (double.IsNaN(r.Accuracy) || double.IsInfinity(r.Accuracy)) r.Accuracy = 0;
            r.Accuracy = Math.Clamp(r.Accuracy, 0, 100);
            if (r.XpEarned < 0) r.XpEarned = 0;
            if (r.ErrorCount < 0) r.ErrorCount = 0;
            if (r.TotalChars < 0) r.TotalChars = 0;
            if (r.DurationSeconds < 0) r.DurationSeconds = 0;
            r.Difficulty = Math.Clamp(r.Difficulty, 1, 7);

            // v0.4.0 record fields — clamp note length, validate DeclaredIntent enum
            if (r.Note?.Length > 280)
                r.Note = r.Note[..280];
            if (r.DeclaredIntent.HasValue &&
                !Enum.IsDefined(typeof(UserIntent), r.DeclaredIntent.Value))
                r.DeclaredIntent = null;
        }

        // Longitudinal data (v0.3.0+)
        blob.Longitudinal ??= new();
        blob.Longitudinal.TrendsByLanguage ??= new();
        blob.Longitudinal.SessionTimestamps ??= new();
        blob.Longitudinal.WeaknessSnapshots ??= new();

        // Per-language trend safety (v0.5.0) — ensure no NaN/Inf in rolling data
        foreach (var (_, trend) in blob.Longitudinal.TrendsByLanguage)
        {
            trend.RecentWpm ??= new();
            trend.RecentAccuracy ??= new();
            trend.RecentWpm.RemoveAll(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0);
            trend.RecentAccuracy.RemoveAll(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0 || v > 100);
            if (trend.TotalSessions < 0) trend.TotalSessions = trend.RecentWpm.Count;
        }

        // Signal policy (v1.0.0) — ensure it exists and booleans are valid
        blob.Settings.SignalPolicy ??= new();

        // Collections
        blob.FavoriteSnippetIds ??= new();
        blob.LastPracticedByLanguage ??= new();
    }

    /// <summary>
    /// Save persisted data with backup.
    /// </summary>
    public void Save(PersistedBlob blob)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            // Create backup of existing data first
            if (settings.Values.TryGetValue(Key, out var existing) && existing is string existingJson)
            {
                settings.Values[BackupKey] = existingJson;
            }

            // Save new data
            var json = JsonSerializer.Serialize(blob, SerializerOptions);
            settings.Values[Key] = json;
            settings.Values[VersionKey] = CurrentSchemaVersion;

            _cachedBlob = blob;
            _isDirty = false;

            DataSaved?.Invoke(this, new DataSavedEventArgs(true, json.Length));
        }
        catch (Exception ex)
        {
            DataSaved?.Invoke(this, new DataSavedEventArgs(false, error: ex.Message));
        }
    }

    /// <summary>
    /// Marks data as dirty (needs saving).
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Save only if there are unsaved changes.
    /// </summary>
    public void SaveIfDirty()
    {
        if (_isDirty && _cachedBlob != null)
        {
            Save(_cachedBlob);
        }
    }

    /// <summary>
    /// Export data to JSON string.
    /// </summary>
    public string? Export(PersistedBlob blob, bool indented = true)
    {
        try
        {
            var options = indented ? IndentedOptions : SerializerOptions;
            return JsonSerializer.Serialize(blob, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Import data from JSON string.
    /// </summary>
    public PersistedBlob? Import(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PersistedBlob>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reset all persisted data.
    /// </summary>
    public void Reset()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(Key);
            settings.Values.Remove(BackupKey);
            _cachedBlob = new PersistedBlob();
            _isDirty = false;
        }
        catch { }
    }

    /// <summary>
    /// Restore data from backup.
    /// </summary>
    public bool RestoreFromBackup()
    {
        var backup = TryLoadBackup();
        if (backup != null)
        {
            Save(backup);
            return true;
        }
        return false;
    }

    private PersistedBlob? TryLoadBackup()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(BackupKey, out var obj) && obj is string json)
            {
                return JsonSerializer.Deserialize<PersistedBlob>(json, SerializerOptions);
            }
        }
        catch { }
        return null;
    }

    private static int GetSchemaVersion(ApplicationDataContainer settings)
    {
        if (settings.Values.TryGetValue(VersionKey, out var v) && v is int version)
        {
            return version;
        }
        return 1; // Assume v1 if no version stored
    }

    private void MigrateSchema(ApplicationDataContainer settings, int fromVersion)
    {
        // v2 → v3: Added MistakeHeatmap to Profile, TypingRules to AppSettings,
        //          XpEarned + Difficulty to SessionRecord.
        //          All new fields have defaults, so existing JSON deserializes cleanly.
        //          WeakChars is preserved for backward compat.
        //
        // No data transformation needed — System.Text.Json handles missing properties
        // by using default values from the class definitions.

        if (fromVersion < 3)
        {
            // Seed heatmap from legacy WeakChars if they exist
            try
            {
                if (settings.Values.TryGetValue(Key, out var obj) && obj is string json)
                {
                    var blob = JsonSerializer.Deserialize<PersistedBlob>(json, SerializerOptions);
                    if (blob?.Profile?.WeakChars?.Count > 0 && blob.Profile.Heatmap.Records.Count == 0)
                    {
                        // Seed each legacy weak char with a synthetic miss record
                        foreach (var c in blob.Profile.WeakChars)
                        {
                            blob.Profile.Heatmap.RecordMiss(c, null);
                        }

                        // Save migrated data
                        var migratedJson = JsonSerializer.Serialize(blob, SerializerOptions);
                        settings.Values[Key] = migratedJson;
                    }
                }
            }
            catch
            {
                // Migration is best-effort — don't crash on corrupt data
            }
        }

        settings.Values[VersionKey] = CurrentSchemaVersion;
    }
}

#region Event Args

public class DataLoadedEventArgs : EventArgs
{
    public bool Success { get; }
    public PersistedBlob? Data { get; }
    public bool FromBackup { get; }
    public string? Error { get; }

    public DataLoadedEventArgs(bool success, PersistedBlob? data, bool fromBackup = false, string? error = null)
    {
        Success = success;
        Data = data;
        FromBackup = fromBackup;
        Error = error;
    }
}

public class DataSavedEventArgs : EventArgs
{
    public bool Success { get; }
    public int BytesSaved { get; }
    public string? Error { get; }

    public DataSavedEventArgs(bool success, int bytesSaved = 0, string? error = null)
    {
        Success = success;
        BytesSaved = bytesSaved;
        Error = error;
    }
}

#endregion
