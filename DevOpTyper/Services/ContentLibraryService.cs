using System.Text.Json;
using DevOpTyper.Content.Abstractions;
using DevOpTyper.Content.Models;
using DevOpTyper.Content.Services;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Unified content service backed by meta-content-system v1.0.0.
/// Replaces SnippetService, UserContentService, and CommunityContentService.
/// All content flows through CodeItem → ContentBridge → Snippet.
/// </summary>
public sealed class ContentLibraryService
{
    private readonly BuiltinOverlayStore _overlays = new();
    private readonly JsonLibraryIndexStore _indexStore = new();
    private readonly MetricCalculator _metrics = new();
    private readonly LanguageDetector _detector = new();
    private readonly UserContentService _userContent = new();
    private readonly CommunityContentService _communityContent = new();
    private readonly List<CodeItem> _allItems = new();
    private InMemoryContentLibrary? _library;
    private bool _initialized;

    /// <summary>
    /// Path to the persistent library index (user + corpus items only).
    /// </summary>
    private static string IndexPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DevOpTyper",
        ExtensionBoundary.LibraryIndexFile);

    /// <summary>
    /// Supported language icons for UI display.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = "\U0001F40D",
        ["javascript"] = "\U0001F4DC",
        ["csharp"] = "\U0001F537",
        ["java"] = "\u2615",
        ["sql"] = "\U0001F5C3\uFE0F",
        ["bash"] = "\U0001F4BB",
        ["typescript"] = "\U0001F4A0",
        ["rust"] = "\U0001F980",
        ["go"] = "\U0001F439",
        ["cpp"] = "\u2699\uFE0F"
    };

    /// <summary>
    /// The underlying IContentLibrary for direct queries.
    /// </summary>
    public IContentLibrary Library => _library ?? new InMemoryContentLibrary(Array.Empty<CodeItem>());

    /// <summary>
    /// Initialize the content library:
    /// 1. Load built-in snippets → CodeItems (source=builtin) + overlays
    /// 2. Load library.index.json → user/corpus CodeItems
    /// 3. Merge all into InMemoryContentLibrary
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Step 1: Load built-in overlays
        _overlays.Initialize();

        // Step 2: Convert built-in snippets to CodeItems
        LoadBuiltinCodeItems();

        // Step 3: Load persisted user/corpus items
        LoadPersistedIndex();

        // Step 4: Build the unified library
        RebuildLibrary();

        // Step 5: Initialize legacy sub-services for directory/status access
        _userContent.Initialize();
        _communityContent.Initialize();
    }

    private void LoadBuiltinCodeItems()
    {
        var snippetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Snippets");
        if (!Directory.Exists(snippetsDir)) return;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var file in Directory.GetFiles(snippetsDir, "*.json"))
        {
            try
            {
                var language = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                var json = File.ReadAllText(file);
                var snippets = JsonSerializer.Deserialize<List<Snippet>>(json, options);
                if (snippets == null) continue;

                foreach (var snippet in snippets)
                {
                    if (string.IsNullOrWhiteSpace(snippet.Code)) continue;

                    var lang = string.IsNullOrWhiteSpace(snippet.Language)
                        ? language
                        : snippet.Language.ToLowerInvariant();

                    var normalized = Normalizer.Normalize(snippet.Code, ensureTrailingNewline: true);
                    var metrics = _metrics.Compute(normalized);
                    var id = ContentId.From(lang, normalized);

                    _allItems.Add(new CodeItem(
                        Id: id,
                        Language: lang,
                        Source: "builtin",
                        Title: snippet.Title ?? $"{lang} snippet",
                        Code: normalized,
                        Metrics: metrics,
                        CreatedUtc: DateTimeOffset.UtcNow
                    ));
                }
            }
            catch
            {
                // Silently skip malformed files
            }
        }
    }

    private void LoadPersistedIndex()
    {
        try
        {
            var index = _indexStore.Load(IndexPath);

            int userCount = 0;
            int corpusCount = 0;

            foreach (var item in index.Items)
            {
                // Skip duplicates (built-in items already loaded)
                if (_allItems.Any(i => i.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Enforce library size limits
                if (item.Source == "user" && ++userCount > ExtensionBoundary.MaxLibraryUserItems)
                    continue;
                if (item.Source == "corpus" && ++corpusCount > ExtensionBoundary.MaxLibraryCorpusItems)
                    continue;

                _allItems.Add(item);
            }
        }
        catch
        {
            // Missing or corrupt index — built-ins still available
        }
    }

    private void RebuildLibrary()
    {
        _library = new InMemoryContentLibrary(_allItems);
    }

    // ─────────────────────────────────────────────
    //  Query API (mirrors SnippetService)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gets all snippets for a language, optionally filtered by difficulty.
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippets(string language, int? difficulty = null)
    {
        Initialize();
        var items = Library.Query(new ContentQuery { Language = language });
        var snippets = items.Select(i => ContentBridge.ToSnippet(i, _overlays.GetOverlay(i.Id))).ToList();

        if (difficulty.HasValue)
            return snippets.Where(s => s.Difficulty == difficulty.Value).ToList();

        return snippets;
    }

    /// <summary>
    /// Gets all available language tracks.
    /// </summary>
    public IReadOnlyList<LanguageTrack> GetLanguageTracks()
    {
        Initialize();
        var languages = Library.Languages();
        var tracks = new List<LanguageTrack>();

        foreach (var lang in languages)
        {
            var items = Library.Query(new ContentQuery { Language = lang });
            var snippets = items.Select(i => ContentBridge.ToSnippet(i, _overlays.GetOverlay(i.Id))).ToList();

            tracks.Add(new LanguageTrack
            {
                Id = lang,
                DisplayName = char.ToUpper(lang[0]) + lang[1..],
                Icon = LanguageIcons.GetValueOrDefault(lang, "\U0001F4DD"),
                SnippetCount = snippets.Count,
                HasUserContent = items.Any(i => i.Source != "builtin"),
                AvailableDifficulties = snippets
                    .Select(s => s.DifficultyLabel)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToArray()
            });
        }

        return tracks;
    }

    /// <summary>
    /// Gets all available languages.
    /// </summary>
    public IReadOnlyList<string> Languages()
    {
        Initialize();
        return Library.Languages();
    }

    /// <summary>
    /// Gets snippet count for a language.
    /// </summary>
    public int GetSnippetCount(string language)
    {
        Initialize();
        return Library.Query(new ContentQuery { Language = language }).Count;
    }

    /// <summary>
    /// Gets a snippet by its ID (legacy or content-hash).
    /// </summary>
    public Snippet? GetSnippetById(string language, string id)
    {
        var snippets = GetSnippets(language);
        return snippets.FirstOrDefault(s =>
            s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets snippets by topic/tag (only works for built-in content with overlays).
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippetsByTopic(string language, string topic)
    {
        var snippets = GetSnippets(language);
        return snippets.Where(s =>
            s.Topics.Contains(topic, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets a random snippet for the given language.
    /// </summary>
    public Snippet? GetRandomSnippet(string language)
    {
        var snippets = GetSnippets(language);
        if (snippets.Count == 0) return null;
        return snippets[Random.Shared.Next(snippets.Count)];
    }

    /// <summary>
    /// Gets a random snippet filtered by difficulty.
    /// </summary>
    public Snippet? GetRandomSnippet(string language, int difficulty)
    {
        var snippets = GetSnippets(language, difficulty);
        if (snippets.Count == 0) return null;
        return snippets[Random.Shared.Next(snippets.Count)];
    }

    /// <summary>
    /// Gets a snippet based on user's skill rating (simple selection).
    /// </summary>
    public Snippet GetSnippet(string language, Dictionary<string, int> ratingByLanguage)
    {
        language = string.IsNullOrWhiteSpace(language) ? "python" : language;

        var snippets = GetSnippets(language);
        if (snippets.Count == 0)
            return new Snippet { Title = "No snippets found", Code = "// Add snippets or import code" };

        int rating = ratingByLanguage.TryGetValue(language, out var r) ? r : 1200;
        int targetDifficulty = rating switch
        {
            < 1100 => 2,
            < 1300 => 3,
            < 1500 => 4,
            _ => 5
        };

        var candidates = snippets.Where(s => Math.Abs(s.Difficulty - targetDifficulty) <= 1).ToList();
        if (candidates.Count == 0) candidates = snippets.ToList();

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    // ─────────────────────────────────────────────
    //  Persistence
    // ─────────────────────────────────────────────

    /// <summary>
    /// Persist user/corpus items to library.index.json.
    /// Built-in items are never persisted (rebuilt from assets each startup).
    /// </summary>
    public void SaveIndex()
    {
        var userCorpusItems = _allItems.Where(i => i.Source != "builtin").ToList();
        var index = new LibraryIndex
        {
            Version = 1,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Items = userCorpusItems
        };
        _indexStore.Save(IndexPath, index);
    }

    // ─────────────────────────────────────────────
    //  Add Code flows
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds user-pasted code to the library.
    /// Detects language, normalizes, computes metrics, deduplicates.
    /// Returns the resulting Snippet, or null if rejected (too long, duplicate, limit reached).
    /// </summary>
    public (Snippet? Snippet, string? Error) AddPastedCode(string code, string? languageHint = null)
    {
        Initialize();

        if (string.IsNullOrWhiteSpace(code))
            return (null, "No code to add");

        if (code.Length > ExtensionBoundary.MaxPasteLength)
            return (null, $"Code exceeds {ExtensionBoundary.MaxPasteLength} character limit ({code.Length} chars)");

        // Check user item count limit
        int userCount = _allItems.Count(i => i.Source == "user");
        if (userCount >= ExtensionBoundary.MaxLibraryUserItems)
            return (null, $"Library limit reached ({ExtensionBoundary.MaxLibraryUserItems} user items)");

        var normalized = Normalizer.Normalize(code, ensureTrailingNewline: true);
        var lang = _detector.Detect(null, languageHint, normalized);
        var metrics = _metrics.Compute(normalized);
        var id = ContentId.From(lang, normalized);

        // Dedup check
        if (_allItems.Any(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
        {
            // Return the existing snippet instead of adding a duplicate
            var existing = _allItems.First(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return (ContentBridge.ToSnippet(existing, _overlays.GetOverlay(existing.Id)), null);
        }

        // Derive title from first non-empty line
        var firstLine = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Pasted code";
        var title = firstLine.Length > 60 ? firstLine[..57] + "..." : firstLine;

        var item = new CodeItem(
            Id: id,
            Language: lang,
            Source: "user",
            Title: title,
            Code: normalized,
            Metrics: metrics,
            CreatedUtc: DateTimeOffset.UtcNow
        );

        _allItems.Add(item);
        RebuildLibrary();
        SaveIndex();

        return (ContentBridge.ToSnippet(item, null), null);
    }

    /// <summary>
    /// Imports code files from a local folder into the library as corpus items.
    /// Uses LibraryIndexBuilder pipeline: extract → normalize → metrics → dedup.
    /// Returns the number of new items added.
    /// </summary>
    public async Task<(int Added, string? Error)> ImportFolder(string folderPath, CancellationToken ct = default)
    {
        Initialize();

        if (!Directory.Exists(folderPath))
            return (0, "Folder not found");

        int corpusCount = _allItems.Count(i => i.Source == "corpus");
        int remaining = ExtensionBoundary.MaxLibraryCorpusItems - corpusCount;
        if (remaining <= 0)
            return (0, $"Corpus limit reached ({ExtensionBoundary.MaxLibraryCorpusItems} items)");

        try
        {
            var source = new FolderContentSource(folderPath, ExtensionBoundary.MaxImportFileSize);
            var extractor = new DefaultExtractor();
            var builder = new LibraryIndexBuilder(extractor, _metrics);
            var index = await builder.BuildAsync(source, ct);

            int added = 0;
            foreach (var item in index.Items)
            {
                if (added >= remaining) break;

                // Skip duplicates
                if (_allItems.Any(i => i.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;

                _allItems.Add(item);
                added++;
            }

            if (added > 0)
            {
                RebuildLibrary();
                SaveIndex();
            }

            return (added, null);
        }
        catch (OperationCanceledException)
        {
            return (0, "Import cancelled");
        }
        catch (Exception ex)
        {
            return (0, $"Import failed: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────
    //  Sub-service access (for backward compat)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Access the overlay store for legacy ID resolution.
    /// </summary>
    public BuiltinOverlayStore Overlays => _overlays;

    /// <summary>
    /// Exposes the user content service for directory management and status display.
    /// Will be removed when Paste Code flow replaces filesystem-based user content.
    /// </summary>
    public UserContentService UserContent => _userContent;

    /// <summary>
    /// Exposes the community content service for directory management and status display.
    /// Will be removed when Import Folder flow replaces filesystem-based community content.
    /// </summary>
    public CommunityContentService CommunityContent => _communityContent;
}
