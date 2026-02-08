using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Discovers and loads community-shared practice material.
///
/// Community content lives in a dedicated directory (CommunityContent/) alongside
/// the app's data. It follows the same JSON schema as built-in and user-authored
/// snippets. Community content is local, explicit, and anonymous — no accounts,
/// no origin tracking, no identity.
///
/// At runtime, community content is indistinguishable from user-authored content.
/// Both receive IsUserAuthored = true. The filesystem separation exists only so
/// users can organize "mine" vs "from others" on disk.
///
/// If the CommunityContent directory doesn't exist, this service does nothing.
/// Zero scanning, zero overhead.
/// </summary>
public sealed class CommunityContentService
{
    private readonly Dictionary<string, List<Snippet>> _communitySnippets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _loadErrors = new();
    private bool _initialized;

    /// <summary>
    /// The directory where community-shared snippets live.
    /// Null if no community content directory exists.
    /// </summary>
    public string? CommunityContentPath { get; private set; }

    /// <summary>
    /// Whether any community content was found and loaded.
    /// </summary>
    public bool HasCommunityContent => _communitySnippets.Values.Any(v => v.Count > 0);

    /// <summary>
    /// Number of community snippet files successfully loaded.
    /// </summary>
    public int LoadedFileCount { get; private set; }

    /// <summary>
    /// Total number of community snippets loaded across all files.
    /// </summary>
    public int TotalSnippetCount => _communitySnippets.Values.Sum(v => v.Count);

    /// <summary>
    /// Errors encountered during loading. Displayed to the user
    /// if they want to debug community content — never blocks the app.
    /// </summary>
    public IReadOnlyList<string> LoadErrors => _loadErrors;

    /// <summary>
    /// Discovers and loads community snippets from the CommunityContent directory.
    /// If the directory doesn't exist, this is a complete no-op — zero scanning,
    /// zero overhead. The app remains fully functional without any community content.
    /// Solo use is first-class.
    /// Safe to call multiple times — only initializes once.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var communityDir = Path.Combine(appDataDir, ExtensionBoundary.CommunityContentDir);

        // Safety: reject if the path somehow resolves inside the app directory
        var appBase = AppContext.BaseDirectory;
        if (communityDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase))
        {
            _loadErrors.Add("Community content directory must not be inside the app directory");
            return;
        }

        if (!Directory.Exists(communityDir))
        {
            CommunityContentPath = null;
            return;
        }

        CommunityContentPath = communityDir;

        // Scan snippets from top-level and one level of subdirectories
        var snippetsDir = communityDir; // Snippets can be at root or in snippets/ subfolder
        var snippetsDirNested = Path.Combine(communityDir, "snippets");

        var files = new List<string>();

        // Check for snippets/ subfolder first (bundle-imported layout)
        if (Directory.Exists(snippetsDirNested))
        {
            files.AddRange(Directory.GetFiles(snippetsDirNested, $"*{ExtensionBoundary.SnippetFileExtension}"));
            foreach (var subDir in Directory.GetDirectories(snippetsDirNested))
            {
                files.AddRange(Directory.GetFiles(subDir, $"*{ExtensionBoundary.SnippetFileExtension}"));
            }
        }

        // Also scan top-level .json files (manually placed)
        files.AddRange(Directory.GetFiles(communityDir, $"*{ExtensionBoundary.SnippetFileExtension}")
            .Where(f => !Path.GetFileName(f).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
                     && !Path.GetFileName(f).Equals("signals.json", StringComparison.OrdinalIgnoreCase)));

        // One level of subdirectories at top level (excluding known folders)
        foreach (var subDir in Directory.GetDirectories(communityDir))
        {
            var dirName = Path.GetFileName(subDir).ToLowerInvariant();
            if (dirName is "snippets" or "configs" or "signals") continue;

            files.AddRange(Directory.GetFiles(subDir, $"*{ExtensionBoundary.SnippetFileExtension}"));
        }

        foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase)
                     .Take(ExtensionBoundary.MaxCommunitySnippetFiles))
        {
            LoadCommunityFile(file);
        }
    }

    /// <summary>
    /// Gets all community snippets for a language.
    /// Returns an empty list if no community content exists for this language.
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippets(string language)
    {
        var lang = language?.ToLowerInvariant() ?? "";
        return _communitySnippets.TryGetValue(lang, out var list) ? list : [];
    }

    /// <summary>
    /// Gets all languages that have community-shared snippets.
    /// </summary>
    public IReadOnlyList<string> GetCommunityLanguages()
    {
        return _communitySnippets
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => kvp.Key)
            .OrderBy(l => l)
            .ToList();
    }

    /// <summary>
    /// Returns the path where community content lives.
    /// Creates the directory if it doesn't exist yet.
    /// </summary>
    public string EnsureCommunityContentDirectory()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var communityDir = Path.Combine(appDataDir, ExtensionBoundary.CommunityContentDir);

        if (!Directory.Exists(communityDir))
            Directory.CreateDirectory(communityDir);

        CommunityContentPath = communityDir;
        return communityDir;
    }

    /// <summary>
    /// Returns age information about community content files.
    /// Useful for the settings panel to show how old the content is.
    /// Returns null if no community content directory exists.
    /// </summary>
    public (int FileCount, DateTime? OldestFile, DateTime? NewestFile)? GetContentAge()
    {
        if (CommunityContentPath == null || !Directory.Exists(CommunityContentPath))
            return null;

        var files = Directory.GetFiles(CommunityContentPath, "*.json", SearchOption.AllDirectories);
        if (files.Length == 0)
            return (0, null, null);

        DateTime? oldest = null;
        DateTime? newest = null;

        foreach (var file in files)
        {
            var modified = File.GetLastWriteTimeUtc(file);
            if (!oldest.HasValue || modified < oldest.Value)
                oldest = modified;
            if (!newest.HasValue || modified > newest.Value)
                newest = modified;
        }

        return (files.Length, oldest, newest);
    }

    /// <summary>
    /// Returns a human-readable summary of community content state.
    /// Suitable for display in the settings panel.
    /// </summary>
    public string GetContentSummary()
    {
        if (!HasCommunityContent)
            return "No community content";

        var languages = GetCommunityLanguages();
        var snippetCount = TotalSnippetCount;

        var parts = new List<string>
        {
            $"{snippetCount} snippet{(snippetCount != 1 ? "s" : "")}",
            $"{languages.Count} language{(languages.Count != 1 ? "s" : "")}"
        };

        if (LoadedFileCount > 0)
            parts.Add($"{LoadedFileCount} file{(LoadedFileCount != 1 ? "s" : "")}");

        var age = GetContentAge();
        if (age?.OldestFile != null)
        {
            var daysOld = (DateTime.UtcNow - age.Value.OldestFile!.Value).Days;
            if (daysOld > 180)
                parts.Add($"oldest content: {daysOld / 30} months ago");
        }

        return string.Join(" · ", parts);
    }

    private void LoadCommunityFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var snippets = JsonSerializer.Deserialize<List<Snippet>>(json, options);
            if (snippets == null || snippets.Count == 0)
            {
                _loadErrors.Add($"{fileName}: empty or invalid JSON");
                return;
            }

            // Validate against extension boundaries
            var error = ExtensionBoundary.ValidateSnippetFile(snippets, fileName);
            if (error != null)
            {
                _loadErrors.Add(error);
                return;
            }

            // Derive default language from filename
            var defaultLanguage = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

            // Mark all snippets as user-authored — indistinguishable from user content
            // at runtime. No community-origin flag on snippets. Content origin is
            // indistinguishable during practice. This is by design: shared material
            // behaves identically to local material.
            foreach (var s in snippets)
            {
                if (string.IsNullOrEmpty(s.Language))
                    s.Language = defaultLanguage;
                s.IsUserAuthored = true;
            }

            // Group by actual language
            var byLanguage = snippets.GroupBy(s => s.Language.ToLowerInvariant());
            foreach (var group in byLanguage)
            {
                if (!_communitySnippets.TryGetValue(group.Key, out var existing))
                {
                    existing = new List<Snippet>();
                    _communitySnippets[group.Key] = existing;
                }
                existing.AddRange(group);
            }

            LoadedFileCount++;
        }
        catch (JsonException ex)
        {
            _loadErrors.Add($"{fileName}: JSON parse error — {ex.Message}");
        }
        catch (Exception ex)
        {
            _loadErrors.Add($"{fileName}: {ex.Message}");
        }
    }
}
