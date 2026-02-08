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
    /// Selects the next snippet using adaptive difficulty and weakness tracking (v0.3.0).
    /// Uses DifficultyProfile for trend-aware difficulty targeting and WeaknessReport
    /// for improvement-aware weakness scoring.
    /// </summary>
    public Snippet SelectAdaptive(
        string language, Profile profile,
        DifficultyProfile? difficultyProfile,
        WeaknessReport? weaknessReport)
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

        if (candidates.Count == 0)
        {
            _recentSnippetIds.Clear();
            candidates = allSnippets;
        }

        // Use adaptive difficulty if available, else fall back to static
        int targetDifficulty;
        int minDiff, maxDiff;

        if (difficultyProfile != null)
        {
            targetDifficulty = difficultyProfile.TargetDifficulty;
            minDiff = difficultyProfile.MinDifficulty;
            maxDiff = difficultyProfile.MaxDifficulty;
        }
        else
        {
            int rating = profile.GetRating(language);
            targetDifficulty = GetTargetDifficulty(rating);
            minDiff = Math.Max(1, targetDifficulty - 1);
            maxDiff = Math.Min(5, targetDifficulty + 1);
        }

        // Score each candidate with enriched signals
        var scored = candidates.Select(s => new ScoredSnippet
        {
            Snippet = s,
            Score = ComputeAdaptiveScore(s, targetDifficulty, minDiff, maxDiff, profile, weaknessReport)
        })
        .OrderByDescending(s => s.Score)
        .ToList();

        // Select from top candidates
        var topCandidates = scored.Take(Math.Min(5, scored.Count)).ToList();
        var selected = topCandidates[_random.Next(topCandidates.Count)].Snippet;

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

    /// <summary>
    /// Enhanced scoring that uses DifficultyProfile range and WeaknessReport trajectory.
    /// </summary>
    private double ComputeAdaptiveScore(
        Snippet snippet, int targetDifficulty, int minDiff, int maxDiff,
        Profile profile, WeaknessReport? weaknessReport)
    {
        double score = 100.0;

        // Difficulty scoring with range awareness
        if (snippet.Difficulty < minDiff || snippet.Difficulty > maxDiff)
        {
            // Outside acceptable range — heavy penalty
            int gap = snippet.Difficulty < minDiff
                ? minDiff - snippet.Difficulty
                : snippet.Difficulty - maxDiff;
            score -= gap * 30;
        }
        else
        {
            // Inside range — reward proximity to target
            int gap = Math.Abs(snippet.Difficulty - targetDifficulty);
            score -= gap * 10;
        }

        // Weakness scoring with trajectory awareness (v0.3.0)
        var snippetSpecials = snippet.SpecialChars;
        if (weaknessReport != null && weaknessReport.HasData)
        {
            foreach (var item in weaknessReport.Items)
            {
                if (!snippetSpecials.Contains(item.Character)) continue;

                // Base bonus from error rate
                double bonus = item.CurrentErrorRate * 30;

                // Trajectory multiplier — focus more on worsening/new weaknesses
                bonus *= item.Trajectory switch
                {
                    WeaknessTrajectory.Worsening => 1.5,  // Extra focus
                    WeaknessTrajectory.New => 1.3,         // Need attention
                    WeaknessTrajectory.Steady => 1.0,      // Normal
                    WeaknessTrajectory.Improving => 0.5,   // Less urgency
                    _ => 1.0
                };

                score += bonus;
            }
        }
        else
        {
            // Fall back to raw heatmap scoring (same as v0.2.0)
            if (profile.Heatmap.Records.Count > 0)
            {
                foreach (var c in snippetSpecials)
                {
                    double errorRate = profile.Heatmap.GetErrorRate(c);
                    if (errorRate > 0.05)
                    {
                        score += errorRate * 40;
                    }
                }
            }
            else
            {
                var weakChars = profile.WeakChars;
                int weakOverlap = weakChars.Count(wc => snippetSpecials.Contains(wc));
                score += weakOverlap * 10;
            }
        }

        // Length preference for lower levels
        if (profile.Level < 5 && snippet.CharCount > 200)
        {
            score -= 10;
        }

        // Randomness
        score += _random.NextDouble() * 10;

        return score;
    }

    /// <summary>
    /// Maps a skill rating to target difficulty (public for external callers).
    /// </summary>
    public static int GetTargetDifficultyStatic(int rating)
    {
        return GetTargetDifficulty(rating);
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
