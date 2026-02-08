using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Service for loading and selecting code snippets for typing practice.
/// Transparently merges built-in and user-authored content.
/// If no user content exists, behavior is identical to v0.5.0.
/// </summary>
public sealed class SnippetService
{
    private readonly Dictionary<string, List<Snippet>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LanguageTrack> _languageTracks = new();
    private readonly UserContentService _userContent = new();
    private bool _initialized = false;

    /// <summary>
    /// Exposes the user content service for UI queries (e.g., "Open folder").
    /// </summary>
    public UserContentService UserContent => _userContent;

    /// <summary>
    /// Supported language icons for UI display.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = "🐍",
        ["javascript"] = "📜",
        ["csharp"] = "🔷",
        ["java"] = "☕",
        ["sql"] = "🗃️",
        ["bash"] = "💻",
        ["typescript"] = "💠",
        ["rust"] = "🦀",
        ["go"] = "🐹",
        ["cpp"] = "⚙️"
    };

    /// <summary>
    /// Initializes the service by scanning available snippet files.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        var snippetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Snippets");
        if (!Directory.Exists(snippetsDir))
        {
            _initialized = true;
            return;
        }

        foreach (var file in Directory.GetFiles(snippetsDir, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            var snippets = Load(language);
            
            if (snippets.Count > 0)
            {
                var difficulties = snippets
                    .Select(s => s.DifficultyLabel)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToArray();

                _languageTracks.Add(new LanguageTrack
                {
                    Id = language,
                    DisplayName = char.ToUpper(language[0]) + language.Substring(1),
                    Icon = LanguageIcons.GetValueOrDefault(language, "📝"),
                    SnippetCount = snippets.Count,
                    AvailableDifficulties = difficulties
                });
            }
        }

        // Merge user-authored content if any exists.
        // This is a no-op if the UserSnippets directory doesn't exist.
        _userContent.Initialize();
        if (_userContent.HasUserContent)
        {
            MergeUserContent();
        }

        _initialized = true;
    }

    /// <summary>
    /// Merges user-authored snippets into the cache and language tracks.
    /// User snippets are appended to existing language lists or create
    /// new language entries. All selection methods automatically include them.
    /// </summary>
    private void MergeUserContent()
    {
        foreach (var language in _userContent.GetUserLanguages())
        {
            var userSnippets = _userContent.GetSnippets(language);
            if (userSnippets.Count == 0) continue;

            // Merge into cache
            if (!_cache.TryGetValue(language, out var existing))
            {
                existing = new List<Snippet>();
                _cache[language] = existing;
            }
            existing.AddRange(userSnippets);

            // Update or create language track
            var track = _languageTracks.FirstOrDefault(t =>
                t.Id.Equals(language, StringComparison.OrdinalIgnoreCase));

            if (track != null)
            {
                // Existing language — update count and difficulties
                track.SnippetCount = existing.Count;
                track.AvailableDifficulties = existing
                    .Select(s => s.DifficultyLabel)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToArray();
            }
            else
            {
                // New language from user content
                _languageTracks.Add(new LanguageTrack
                {
                    Id = language,
                    DisplayName = char.ToUpper(language[0]) + language[1..],
                    Icon = LanguageIcons.GetValueOrDefault(language, "📝"),
                    SnippetCount = userSnippets.Count,
                    AvailableDifficulties = userSnippets
                        .Select(s => s.DifficultyLabel)
                        .Distinct()
                        .OrderBy(d => d)
                        .ToArray()
                });
            }
        }
    }

    /// <summary>
    /// Gets all available language tracks.
    /// </summary>
    public IReadOnlyList<LanguageTrack> GetLanguageTracks()
    {
        Initialize();
        return _languageTracks;
    }

    /// <summary>
    /// Gets a snippet based on user's skill rating for adaptive difficulty.
    /// </summary>
    public Snippet GetSnippet(string language, Dictionary<string, int> ratingByLanguage)
    {
        language = string.IsNullOrWhiteSpace(language) ? "python" : language;

        var list = Load(language);
        if (list.Count == 0)
        {
            return new Snippet { Title = "No snippets found", Code = "// Add snippets in Assets/Snippets" };
        }

        // Simple selection: map rating to difficulty window
        int rating = ratingByLanguage.TryGetValue(language, out var r) ? r : 1200;
        int targetDifficulty = rating switch
        {
            < 1100 => 2,
            < 1300 => 3,
            < 1500 => 4,
            _ => 5
        };

        var candidates = list.Where(s => Math.Abs(s.Difficulty - targetDifficulty) <= 1).ToList();
        if (candidates.Count == 0) candidates = list;

        var rnd = Random.Shared.Next(candidates.Count);
        return candidates[rnd];
    }

    /// <summary>
    /// Gets all snippets for a language, optionally filtered by difficulty.
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippets(string language, int? difficulty = null)
    {
        var list = Load(language);
        if (difficulty.HasValue)
        {
            return list.Where(s => s.Difficulty == difficulty.Value).ToList();
        }
        return list;
    }

    /// <summary>
    /// Gets snippets by topic/tag.
    /// </summary>
    public IReadOnlyList<Snippet> GetSnippetsByTopic(string language, string topic)
    {
        var list = Load(language);
        return list.Where(s => s.Topics.Contains(topic, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets a random snippet for the given language.
    /// </summary>
    public Snippet? GetRandomSnippet(string language)
    {
        language = string.IsNullOrWhiteSpace(language) ? "python" : language;
        var list = Load(language);
        if (list.Count == 0) return null;
        return list[Random.Shared.Next(list.Count)];
    }

    /// <summary>
    /// Gets a random snippet filtered by difficulty.
    /// </summary>
    public Snippet? GetRandomSnippet(string language, int difficulty)
    {
        var list = Load(language).Where(s => s.Difficulty == difficulty).ToList();
        if (list.Count == 0) return null;
        return list[Random.Shared.Next(list.Count)];
    }

    /// <summary>
    /// Gets snippet count for a language.
    /// </summary>
    public int GetSnippetCount(string language) => Load(language).Count;

    private List<Snippet> Load(string language)
    {
        if (_cache.TryGetValue(language, out var cached)) return cached;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Snippets", $"{language.ToLowerInvariant()}.json");
            if (!File.Exists(path))
            {
                _cache[language] = new List<Snippet>();
                return _cache[language];
            }

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var list = JsonSerializer.Deserialize<List<Snippet>>(json, options) ?? new List<Snippet>();
            
            // Ensure language is set on all snippets
            foreach (var s in list)
            {
                if (string.IsNullOrEmpty(s.Language))
                    s.Language = language;
            }
            
            _cache[language] = list;
            return list;
        }
        catch
        {
            _cache[language] = new List<Snippet>();
            return _cache[language];
        }
    }
}

