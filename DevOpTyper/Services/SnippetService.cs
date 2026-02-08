using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

public sealed class SnippetService
{
    private readonly Dictionary<string, List<Snippet>> _cache = new(StringComparer.OrdinalIgnoreCase);

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
            var list = JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();
            _cache[language] = list;
            return list;
        }
        catch
        {
            _cache[language] = new List<Snippet>();
            return _cache[language];
        }
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
}
