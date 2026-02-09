using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Discovers and loads user-authored practice material.
///
/// User content lives in a dedicated directory (UserSnippets/) alongside
/// the app's data. It follows the same JSON schema as built-in snippets.
/// User content is local, explicit, and portable — no accounts, no uploads,
/// no publishing.
///
/// If the UserSnippets directory doesn't exist, this service does nothing.
/// Zero scanning, zero overhead.
/// </summary>
public sealed class UserContentService
{
    private readonly Dictionary<string, List<Snippet>> _userSnippets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _loadErrors = new();
    private bool _initialized;

    /// <summary>
    /// The directory where user-authored snippets live.
    /// Null if no user content directory exists.
    /// </summary>
    public string? UserSnippetsPath { get; private set; }

    /// <summary>
    /// Whether any user content was found and loaded.
    /// </summary>
    public bool HasUserContent => _userSnippets.Values.Any(v => v.Count > 0);

    /// <summary>
    /// Number of user snippet files successfully loaded.
    /// </summary>
    public int LoadedFileCount { get; private set; }

    /// <summary>
    /// Total number of user snippets loaded across all files.
    /// </summary>
    public int TotalSnippetCount => _userSnippets.Values.Sum(v => v.Count);

    /// <summary>
    /// Errors encountered during loading. Displayed to the user
    /// if they want to debug their content — never blocks the app.
    /// </summary>
    public IReadOnlyList<string> LoadErrors => _loadErrors;

    /// <summary>
    /// Discovers and loads user snippets from the UserSnippets directory.
    /// If the directory doesn't exist, this is a no-op.
    /// Safe to call multiple times — only initializes once.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // User snippets live in %LOCALAPPDATA%, never in the app's install directory.
        // This isolation ensures user content can never corrupt built-in assets.
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var userDir = Path.Combine(appDataDir, ExtensionBoundary.UserSnippetsDir);

        // Safety: reject if the path somehow resolves inside the app directory
        var appBase = AppContext.BaseDirectory;
        if (userDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase))
        {
            _loadErrors.Add("User snippets directory must not be inside the app directory");
            return;
        }

        if (!Directory.Exists(userDir))
        {
            UserSnippetsPath = null;
            return;
        }

        UserSnippetsPath = userDir;

        // Scan top-level files
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(userDir, $"*{ExtensionBoundary.SnippetFileExtension}"));

        // Scan one level of subdirectories — users can organize by folder.
        // Subdirectory name is ignored; language comes from filename or JSON content.
        // No deeper nesting to keep things predictable.
        foreach (var subDir in Directory.GetDirectories(userDir))
        {
            files.AddRange(Directory.GetFiles(subDir, $"*{ExtensionBoundary.SnippetFileExtension}"));
        }

        foreach (var file in files.Take(ExtensionBoundary.MaxUserSnippetFiles))
        {
            LoadUserFile(file);
        }
    }

    /// <summary>
    /// Gets all user snippets for a language.
    /// Returns an empty list if no user content exists for this language.
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippets(string language)
    {
        var lang = language?.ToLowerInvariant() ?? "";
        return _userSnippets.TryGetValue(lang, out var list) ? list : [];
    }

    /// <summary>
    /// Gets all languages that have user-authored snippets.
    /// </summary>
    public IReadOnlyList<string> GetUserLanguages()
    {
        return _userSnippets
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => kvp.Key)
            .OrderBy(l => l)
            .ToList();
    }

    /// <summary>
    /// Returns the path where users should place snippet files.
    /// Creates the directory if it doesn't exist yet.
    /// </summary>
    public string EnsureUserSnippetsDirectory()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var userDir = Path.Combine(appDataDir, ExtensionBoundary.UserSnippetsDir);

        if (!Directory.Exists(userDir))
            Directory.CreateDirectory(userDir);

        UserSnippetsPath = userDir;
        return userDir;
    }

    private void LoadUserFile(string filePath)
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

            // Derive default language from filename (same convention as built-in).
            // But if a snippet declares its own language in JSON, that takes precedence.
            // This lets users create mixed-language collections like "my-favorites.json".
            var defaultLanguage = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

            // Mark all snippets as user-authored and ensure language is set
            foreach (var s in snippets)
            {
                if (string.IsNullOrEmpty(s.Language))
                    s.Language = defaultLanguage;
                s.IsUserAuthored = true;
            }

            // Group by actual language (which may differ from filename)
            var byLanguage = snippets.GroupBy(s => s.Language.ToLowerInvariant());
            foreach (var group in byLanguage)
            {
                if (!_userSnippets.TryGetValue(group.Key, out var existing))
                {
                    existing = new List<Snippet>();
                    _userSnippets[group.Key] = existing;
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
