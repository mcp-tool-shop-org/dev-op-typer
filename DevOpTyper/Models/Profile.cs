namespace DevOpTyper.Models;

public sealed class Profile
{
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;

    // A simple per-language rating; expand later to Elo-like.
    public Dictionary<string, int> RatingByLanguage { get; set; } = new()
    {
        ["python"] = 1200,
        ["java"] = 1200
    };

    // Characters the user struggles with most (legacy — kept for migration compat)
    public HashSet<char> WeakChars { get; set; } = new();

    // Topics the user needs more practice on
    public HashSet<string> WeakTopics { get; set; } = new();

    /// <summary>
    /// Per-character mistake frequency tracking (v0.2.0+).
    /// Replaces WeakChars with frequency-weighted data.
    /// </summary>
    public MistakeHeatmap Heatmap { get; set; } = new();

    public void AddXp(int amount)
    {
        Xp += Math.Max(0, amount);

        // Very simple leveling curve (tune later)
        while (Xp >= XpNeededForNext(Level))
        {
            Xp -= XpNeededForNext(Level);
            Level++;
        }
    }

    /// <summary>
    /// Gets the rating for a specific language, defaulting to 1200.
    /// </summary>
    public int GetRating(string language)
    {
        return RatingByLanguage.TryGetValue(language, out var rating) ? rating : 1200;
    }

    /// <summary>
    /// Updates the rating for a language based on session performance.
    /// </summary>
    public void UpdateRating(string language, double accuracy, double wpm)
    {
        if (!RatingByLanguage.ContainsKey(language))
        {
            RatingByLanguage[language] = 1200;
        }

        // Simple rating adjustment based on performance
        int adjustment = 0;
        
        if (accuracy >= 98 && wpm >= 60)
            adjustment = 25; // Excellent
        else if (accuracy >= 95 && wpm >= 45)
            adjustment = 15; // Good
        else if (accuracy >= 90)
            adjustment = 5;  // Okay
        else if (accuracy < 80)
            adjustment = -10; // Needs practice
        
        RatingByLanguage[language] = Math.Clamp(RatingByLanguage[language] + adjustment, 800, 2000);
    }

    /// <summary>
    /// Records a character that was typed incorrectly.
    /// Updates both legacy WeakChars and the new Heatmap.
    /// </summary>
    public void RecordMiss(char expected, char? actual = null)
    {
        // Legacy: keep WeakChars populated for SmartSnippetSelector compat
        if (!char.IsLetterOrDigit(expected) && !char.IsWhiteSpace(expected))
        {
            WeakChars.Add(expected);
        }

        // New: frequency-weighted tracking
        Heatmap.RecordMiss(expected, actual);
    }

    /// <summary>
    /// Records a character that was typed correctly.
    /// </summary>
    public void RecordHit(char expected)
    {
        Heatmap.RecordHit(expected);

        // Legacy: clear from WeakChars if error rate drops below threshold
        if (Heatmap.GetErrorRate(expected) < 0.10)
        {
            WeakChars.Remove(expected);
        }
    }

    /// <summary>
    /// Records characters that were typed incorrectly to track weak points.
    /// Legacy method — prefer RecordMiss for new code.
    /// </summary>
    [Obsolete("Use RecordMiss(expected, actual) instead")]
    public void RecordWeakChar(char c)
    {
        RecordMiss(c);
    }

    /// <summary>
    /// Removes a character from weak chars if user types it correctly multiple times.
    /// Legacy method — prefer RecordHit for new code.
    /// </summary>
    [Obsolete("Use RecordHit(expected) instead")]
    public void ClearWeakChar(char c)
    {
        WeakChars.Remove(c);
    }

    public static int XpNeededForNext(int level) => 200 + (level * 40);
}
