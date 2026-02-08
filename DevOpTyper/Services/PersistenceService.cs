using System.Text.Json;
using Windows.Storage;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

public sealed class PersistenceService
{
    private const string Key = "DevOpTyper.PersistedBlob.v1";

    public PersistedBlob Load()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(Key, out var obj) && obj is string json && !string.IsNullOrWhiteSpace(json))
            {
                var blob = JsonSerializer.Deserialize<PersistedBlob>(json);
                if (blob is not null) return blob;
            }
        }
        catch { /* ignore */ }

        return new PersistedBlob();
    }

    public void Save(PersistedBlob blob)
    {
        try
        {
            var json = JsonSerializer.Serialize(blob, new JsonSerializerOptions { WriteIndented = false });
            ApplicationData.Current.LocalSettings.Values[Key] = json;
        }
        catch { /* ignore */ }
    }
}
