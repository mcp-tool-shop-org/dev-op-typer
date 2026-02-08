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
    private const int CurrentSchemaVersion = 2;

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
                    _cachedBlob = blob;
                    DataLoaded?.Invoke(this, new DataLoadedEventArgs(true, _cachedBlob));
                    return blob;
                }
            }
        }
        catch (Exception ex)
        {
            // Try loading from backup
            var backup = TryLoadBackup();
            if (backup != null)
            {
                _cachedBlob = backup;
                DataLoaded?.Invoke(this, new DataLoadedEventArgs(true, _cachedBlob, fromBackup: true));
                return backup;
            }

            DataLoaded?.Invoke(this, new DataLoadedEventArgs(false, null, error: ex.Message));
        }

        _cachedBlob = new PersistedBlob();
        return _cachedBlob;
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
        // Future migrations go here
        // For now, just update the version
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
