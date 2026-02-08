using System.Text;
using System.Text.Json;
using DevOpTyper.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DevOpTyper.Services;

/// <summary>
/// Service for exporting and importing user data.
/// </summary>
public sealed class DataExportService
{
    private readonly PersistenceService _persistenceService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DataExportService(PersistenceService persistenceService)
    {
        _persistenceService = persistenceService;
    }

    #region Export Methods

    /// <summary>
    /// Export all data to JSON string.
    /// </summary>
    public string ExportToJson(PersistedBlob data)
    {
        var export = new ExportData
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Profile = data.Profile,
            Settings = data.Settings,
            History = data.History,
            FavoriteSnippetIds = data.FavoriteSnippetIds.ToList()
        };

        return JsonSerializer.Serialize(export, JsonOptions);
    }

    /// <summary>
    /// Export statistics to CSV format.
    /// </summary>
    public string ExportStatsToCsv(SessionHistory history)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Date,Time,Language,Snippet,WPM,Accuracy,Errors,Duration,Hardcore,Perfect");

        // Records
        foreach (var record in history.Records.OrderBy(r => r.CompletedAt))
        {
            sb.AppendLine(string.Join(",",
                record.CompletedAt.ToString("yyyy-MM-dd"),
                record.CompletedAt.ToString("HH:mm:ss"),
                EscapeCsv(record.Language),
                EscapeCsv(record.SnippetTitle),
                record.Wpm.ToString("F1"),
                record.Accuracy.ToString("F1"),
                record.ErrorCount,
                record.DurationSeconds.ToString("F1"),
                record.HardcoreMode ? "Yes" : "No",
                record.IsPerfect ? "Yes" : "No"
            ));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export a summary report.
    /// </summary>
    public string ExportSummaryReport(PersistedBlob data)
    {
        var sb = new StringBuilder();
        var stats = data.History.GetLifetimeStats();
        var bests = data.History.GetPersonalBests();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║             Dev-Op-Typer Progress Report                     ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("═══ PROFILE ═══");
        sb.AppendLine($"Level: {data.Profile.Level}");
        sb.AppendLine($"XP: {data.Profile.Xp} / {Profile.XpNeededForNext(data.Profile.Level)}");
        sb.AppendLine();

        sb.AppendLine("═══ PERSONAL BESTS ═══");
        sb.AppendLine($"Best WPM: {bests.BestWpm:F1}");
        sb.AppendLine($"Best Accuracy: {bests.BestAccuracy:F1}%");
        sb.AppendLine($"Longest Perfect Streak: {bests.LongestStreak}");
        sb.AppendLine($"Fastest Perfect Run: {bests.FastestPerfect:F1} WPM");
        sb.AppendLine($"Most Sessions in a Day: {bests.MostSessionsInDay}");
        sb.AppendLine();

        sb.AppendLine("═══ ALL-TIME STATISTICS ═══");
        sb.AppendLine($"Total Sessions: {stats.TotalSessions}");
        sb.AppendLine($"Total Characters Typed: {stats.TotalCharacters:N0}");
        sb.AppendLine($"Total Time Practiced: {FormatDuration(stats.TotalDurationMinutes)}");
        sb.AppendLine($"Average WPM: {stats.AverageWpm:F1}");
        sb.AppendLine($"Average Accuracy: {stats.AverageAccuracy:F1}%");
        sb.AppendLine($"Perfect Sessions: {stats.PerfectSessions} ({stats.PerfectRate:F1}%)");
        sb.AppendLine($"Hardcore Sessions: {stats.HardcoreSessions}");
        sb.AppendLine();

        sb.AppendLine("═══ LANGUAGE RATINGS ═══");
        foreach (var lang in data.Profile.RatingByLanguage.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"{lang.Key}: {lang.Value}");
        }
        sb.AppendLine();

        if (data.Profile.WeakChars.Count > 0)
        {
            sb.AppendLine("═══ CHARACTERS TO PRACTICE ═══");
            sb.AppendLine(string.Join(" ", data.Profile.WeakChars.Select(c => $"'{c}'")));
            sb.AppendLine();
        }

        sb.AppendLine("════════════════════════════════════════════════════════════════");
        sb.AppendLine("Keep practicing! Every session makes you better.");

        return sb.ToString();
    }

    /// <summary>
    /// Save export to file using file picker.
    /// </summary>
    public async Task<bool> SaveToFileAsync(string content, string suggestedFileName, string fileExtension, nint windowHandle)
    {
        try
        {
            var picker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = suggestedFileName;

            var extension = fileExtension.StartsWith(".") ? fileExtension : $".{fileExtension}";
            picker.FileTypeChoices.Add($"{extension.TrimStart('.').ToUpper()} Files", new List<string> { extension });

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, content);
                return true;
            }
        }
        catch { }

        return false;
    }

    #endregion

    #region Import Methods

    /// <summary>
    /// Import data from JSON string.
    /// </summary>
    public ImportResult ImportFromJson(string json)
    {
        try
        {
            var export = JsonSerializer.Deserialize<ExportData>(json, JsonOptions);
            if (export == null)
            {
                return ImportResult.Failed("Invalid data format");
            }

            // Validate version
            if (export.Version > 1)
            {
                return ImportResult.Failed($"Data version {export.Version} is not supported");
            }

            return ImportResult.Success(export);
        }
        catch (Exception ex)
        {
            return ImportResult.Failed($"Parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply imported data to current data, with merge options.
    /// </summary>
    public void ApplyImport(PersistedBlob current, ExportData import, ImportMode mode)
    {
        switch (mode)
        {
            case ImportMode.Replace:
                // Complete replacement
                if (import.Profile != null) current.Profile = import.Profile;
                if (import.Settings != null) current.Settings = import.Settings;
                if (import.History != null) current.History = import.History;
                if (import.FavoriteSnippetIds != null)
                {
                    current.FavoriteSnippetIds = new HashSet<string>(import.FavoriteSnippetIds);
                }
                break;

            case ImportMode.MergeKeepBetter:
                // Keep the better stats
                if (import.Profile != null)
                {
                    current.Profile.Level = Math.Max(current.Profile.Level, import.Profile.Level);
                    current.Profile.Xp = Math.Max(current.Profile.Xp, import.Profile.Xp);

                    foreach (var (lang, rating) in import.Profile.RatingByLanguage)
                    {
                        if (!current.Profile.RatingByLanguage.TryGetValue(lang, out var existing) || rating > existing)
                        {
                            current.Profile.RatingByLanguage[lang] = rating;
                        }
                    }
                }

                // Merge history
                if (import.History?.Records != null)
                {
                    var existingIds = current.History.Records.Select(r => r.Id).ToHashSet();
                    foreach (var record in import.History.Records)
                    {
                        if (!existingIds.Contains(record.Id))
                        {
                            current.History.AddRecord(record);
                        }
                    }
                }

                // Merge favorites
                if (import.FavoriteSnippetIds != null)
                {
                    foreach (var id in import.FavoriteSnippetIds)
                    {
                        current.FavoriteSnippetIds.Add(id);
                    }
                }
                break;

            case ImportMode.MergeAddOnly:
                // Only add new data, don't overwrite
                if (import.History?.Records != null)
                {
                    var existingIds = current.History.Records.Select(r => r.Id).ToHashSet();
                    foreach (var record in import.History.Records)
                    {
                        if (!existingIds.Contains(record.Id))
                        {
                            current.History.AddRecord(record);
                        }
                    }
                }

                if (import.FavoriteSnippetIds != null)
                {
                    foreach (var id in import.FavoriteSnippetIds)
                    {
                        current.FavoriteSnippetIds.Add(id);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Load import from file using file picker.
    /// </summary>
    public async Task<string?> LoadFromFileAsync(nint windowHandle)
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                return await FileIO.ReadTextAsync(file);
            }
        }
        catch { }

        return null;
    }

    #endregion

    #region Helpers

    private static string FormatDuration(double minutes)
    {
        if (minutes >= 60)
        {
            var hours = (int)(minutes / 60);
            var mins = (int)(minutes % 60);
            return $"{hours}h {mins}m";
        }
        return $"{(int)minutes}m";
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion
}

#region Export/Import Models

/// <summary>
/// Container for exported data.
/// </summary>
public sealed class ExportData
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public Profile? Profile { get; set; }
    public AppSettings? Settings { get; set; }
    public SessionHistory? History { get; set; }
    public List<string>? FavoriteSnippetIds { get; set; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public sealed class ImportResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ExportData? Data { get; private set; }

    public static ImportResult Success(ExportData data) => new() { IsSuccess = true, Data = data };
    public static ImportResult Failed(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// How to handle imported data.
/// </summary>
public enum ImportMode
{
    /// <summary>Replace all current data with imported data.</summary>
    Replace,

    /// <summary>Merge, keeping the better stats.</summary>
    MergeKeepBetter,

    /// <summary>Only add new records, don't modify existing.</summary>
    MergeAddOnly
}

#endregion
