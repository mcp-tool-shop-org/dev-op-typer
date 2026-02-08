using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Provides intelligent snippet selection based on user skill and weak points.
/// </summary>
public sealed class SmartSnippetSelector
{
    private readonly SnippetService _snippetService;
    private readonly Random _random = Random.Shared;

    // Track which snippets the user has recently seen to avoid repeats
    private readonly Queue<string> _recentSnippetIds = new();
    private const int MaxRecentHistory = 10;

    public SmartSnippetSelector(SnippetService snippetService)
    {
        _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
    }

    /// <summary>
    /// Selects the next snippet based on user profile and performance history.
    /// </summary>
    /// <param name="language">Target programming language.</param>
    /// <param name="profile">User's profile with skills and weak chars.</param>
    /// <returns>Selected snippet optimized for learning.</returns>
    public Snippet SelectNext(string language, Profile profile)
    {
        _snippetService.Initialize();
        
        var allSnippets = _snippetService.GetSnippets(language).ToList();
        if (allSnippets.Count == 0)
        {
            return GetFallbackSnippet(language);
        }

        // Filter out recently seen snippets
        var candidates = allSnippets
            .Where(s => !_recentSnippetIds.Contains(s.Id))
            .ToList();

        // If all snippets have been seen recently, reset
        if (candidates.Count == 0)
        {
            _recentSnippetIds.Clear();
            candidates = allSnippets;
        }

        // Get target difficulty based on skill rating
        int rating = profile.GetRating(language);
        int targetDifficulty = GetTargetDifficulty(rating);

        // Score each candidate
        var scored = candidates.Select(s => new ScoredSnippet
        {
            Snippet = s,
            Score = ComputeScore(s, targetDifficulty, profile)
        })
        .OrderByDescending(s => s.Score)
        .ToList();

        // Select from top candidates with some randomness
        var topCandidates = scored.Take(Math.Min(5, scored.Count)).ToList();
        var selected = topCandidates[_random.Next(topCandidates.Count)].Snippet;

        // Track this snippet
        TrackSnippet(selected.Id);

        return selected;
    }

    /// <summary>
    /// Selects a snippet that focuses on specific weak characters.
    /// </summary>
    public Snippet SelectForWeakChars(string language, Profile profile, HashSet<char> weakChars)
    {
        var allSnippets = _snippetService.GetSnippets(language).ToList();
        if (allSnippets.Count == 0)
        {
            return GetFallbackSnippet(language);
        }

        // Score snippets by how many weak chars they contain
        var scored = allSnippets.Select(s =>
        {
            var snippetSpecials = s.SpecialChars;
            int weakCharCount = weakChars.Count(wc => snippetSpecials.Contains(wc));
            return new { Snippet = s, WeakCharScore = weakCharCount };
        })
        .Where(x => x.WeakCharScore > 0)
        .OrderByDescending(x => x.WeakCharScore)
        .ToList();

        if (scored.Count == 0)
        {
            // No snippets with weak chars - fall back to normal selection
            return SelectNext(language, profile);
        }

        // Pick from top 3
        var topCandidates = scored.Take(3).ToList();
        return topCandidates[_random.Next(topCandidates.Count)].Snippet;
    }

    /// <summary>
    /// Selects a snippet for a specific topic.
    /// </summary>
    public Snippet? SelectByTopic(string language, string topic, Profile profile)
    {
        var snippets = _snippetService.GetSnippetsByTopic(language, topic).ToList();
        if (snippets.Count == 0) return null;

        int rating = profile.GetRating(language);
        int targetDifficulty = GetTargetDifficulty(rating);

        // Prefer snippets near target difficulty
        var sorted = snippets
            .OrderBy(s => Math.Abs(s.Difficulty - targetDifficulty))
            .ToList();

        return sorted[_random.Next(Math.Min(3, sorted.Count))];
    }

    private double ComputeScore(Snippet snippet, int targetDifficulty, Profile profile)
    {
        double score = 100.0;

        // Difficulty match (highest weight)
        int diffGap = Math.Abs(snippet.Difficulty - targetDifficulty);
        score -= diffGap * 20; // -20 per difficulty level off

        // Heatmap-weighted weak character bonus (v0.2.0)
        // Characters with higher error rates get proportionally more weight
        var snippetSpecials = snippet.SpecialChars;
        if (profile.Heatmap.Records.Count > 0)
        {
            foreach (var c in snippetSpecials)
            {
                double errorRate = profile.Heatmap.GetErrorRate(c);
                if (errorRate > 0.05) // Only count chars with meaningful error rate
                {
                    // Higher error rate = more bonus for this snippet
                    // errorRate of 0.5 (50% miss) = +20 points per char
                    score += errorRate * 40;
                }
            }
        }
        else
        {
            // Legacy fallback: binary WeakChars for profiles without heatmap data
            var weakChars = profile.WeakChars;
            int weakOverlap = weakChars.Count(wc => snippetSpecials.Contains(wc));
            score += weakOverlap * 10;
        }

        // Slight preference for shorter snippets when learning (reduces overwhelm)
        if (profile.Level < 5 && snippet.CharCount > 200)
        {
            score -= 10;
        }

        // Add small random factor for variety
        score += _random.NextDouble() * 10;

        return score;
    }

    private static int GetTargetDifficulty(int rating)
    {
        return rating switch
        {
            < 1000 => 1, // Beginner
            < 1100 => 2, // Easy
            < 1300 => 3, // Intermediate
            < 1500 => 4, // Advanced
            _ => 5       // Expert
        };
    }

    private void TrackSnippet(string snippetId)
    {
        if (string.IsNullOrEmpty(snippetId)) return;

        _recentSnippetIds.Enqueue(snippetId);
        while (_recentSnippetIds.Count > MaxRecentHistory)
        {
            _recentSnippetIds.Dequeue();
        }
    }

    private static Snippet GetFallbackSnippet(string language) => new()
    {
        Id = "fallback",
        Language = language,
        Title = "No snippets available",
        Code = $"// Add {language} snippets to Assets/Snippets/{language}.json",
        Difficulty = 1
    };

    private record ScoredSnippet
    {
        public Snippet Snippet { get; init; } = null!;
        public double Score { get; init; }
    }
}
