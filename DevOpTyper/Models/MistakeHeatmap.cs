namespace DevOpTyper.Models;

/// <summary>
/// Tracks per-character mistake frequency for identifying weaknesses.
/// Replaces the binary WeakChars HashSet with frequency-weighted data.
/// </summary>
public sealed class MistakeHeatmap
{
    /// <summary>
    /// Per-character mistake tracking. Key = the expected character.
    /// </summary>
    public Dictionary<char, MistakeRecord> Records { get; set; } = new();

    /// <summary>
    /// Default rolling window size. Only the most recent N attempts per character
    /// are considered when computing recent error rates.
    /// </summary>
    public const int DefaultWindowSize = 50;

    /// <summary>
    /// Maximum number of distinct characters tracked. Prevents unbounded growth
    /// from Unicode input or malformed data. Pruning removes least-attempted chars.
    /// </summary>
    public const int MaxTrackedChars = 200;

    /// <summary>
    /// Maximum confusion pairs per character. Keeps only the most frequent pairs.
    /// </summary>
    public const int MaxConfusionPairs = 20;

    /// <summary>
    /// Records a correctly typed character.
    /// </summary>
    public void RecordHit(char expected)
    {
        var record = GetOrCreate(expected);
        record.Hits++;
        record.PushRecent(true);
    }

    /// <summary>
    /// Records an incorrectly typed character.
    /// </summary>
    public void RecordMiss(char expected, char? actual)
    {
        var record = GetOrCreate(expected);
        record.Misses++;
        record.LastMissedAt = DateTime.UtcNow;
        record.PushRecent(false);

        // Track what was typed instead (confusion pairs)
        if (actual.HasValue)
        {
            record.ConfusedWith.TryGetValue(actual.Value, out int count);
            record.ConfusedWith[actual.Value] = count + 1;
        }
    }

    /// <summary>
    /// Gets the all-time error rate for a specific character (0.0 = perfect, 1.0 = always wrong).
    /// </summary>
    public double GetErrorRate(char c)
    {
        if (!Records.TryGetValue(c, out var record)) return 0;
        int total = record.Hits + record.Misses;
        return total > 0 ? (double)record.Misses / total : 0;
    }

    /// <summary>
    /// Gets the recent error rate considering only the last <paramref name="windowSize"/> attempts.
    /// Falls back to all-time rate if fewer recent attempts are tracked.
    /// </summary>
    public double GetRecentErrorRate(char c, int windowSize = DefaultWindowSize)
    {
        if (!Records.TryGetValue(c, out var record)) return 0;
        if (record.RecentAttempts.Count == 0) return GetErrorRate(c);

        var window = record.RecentAttempts.Count <= windowSize
            ? record.RecentAttempts
            : record.RecentAttempts.Skip(record.RecentAttempts.Count - windowSize).ToList();

        if (window.Count == 0) return 0;
        int misses = window.Count(hit => !hit);
        return (double)misses / window.Count;
    }

    /// <summary>
    /// Gets the top N weakest characters by error rate (minimum 5 attempts to qualify).
    /// </summary>
    public List<CharWeakness> GetWeakest(int count = 10, int minAttempts = 5)
    {
        return Records
            .Where(kvp => kvp.Value.Hits + kvp.Value.Misses >= minAttempts)
            .Select(kvp => new CharWeakness
            {
                Character = kvp.Key,
                ErrorRate = GetErrorRate(kvp.Key),
                TotalAttempts = kvp.Value.Hits + kvp.Value.Misses,
                TotalMisses = kvp.Value.Misses,
                Group = GetSymbolGroup(kvp.Key),
                TopConfusion = kvp.Value.ConfusedWith
                    .OrderByDescending(c => c.Value)
                    .Select(c => c.Key)
                    .FirstOrDefault()
            })
            .Where(w => w.ErrorRate > 0)
            .OrderByDescending(w => w.ErrorRate)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets aggregated error rates by symbol group.
    /// </summary>
    public List<GroupWeakness> GetWeakestGroups(int minAttempts = 10)
    {
        return Records
            .GroupBy(kvp => GetSymbolGroup(kvp.Key))
            .Where(g => g.Key != SymbolGroup.Letter) // Letters are usually fine
            .Select(g =>
            {
                int totalHits = g.Sum(kvp => kvp.Value.Hits);
                int totalMisses = g.Sum(kvp => kvp.Value.Misses);
                int total = totalHits + totalMisses;
                return new GroupWeakness
                {
                    Group = g.Key,
                    ErrorRate = total > 0 ? (double)totalMisses / total : 0,
                    TotalAttempts = total,
                    TotalMisses = totalMisses,
                    Characters = g.Select(kvp => kvp.Key).ToList()
                };
            })
            .Where(g => g.TotalAttempts >= minAttempts && g.ErrorRate > 0)
            .OrderByDescending(g => g.ErrorRate)
            .ToList();
    }

    /// <summary>
    /// Gets a flat set of weak characters for backward compatibility with SmartSnippetSelector.
    /// Characters with error rate > 15% and at least 5 attempts qualify.
    /// </summary>
    public HashSet<char> GetWeakCharSet(double threshold = 0.15, int minAttempts = 5)
    {
        var result = new HashSet<char>();
        foreach (var kvp in Records)
        {
            int total = kvp.Value.Hits + kvp.Value.Misses;
            if (total >= minAttempts)
            {
                double errorRate = (double)kvp.Value.Misses / total;
                if (errorRate >= threshold)
                    result.Add(kvp.Key);
            }
        }
        return result;
    }

    /// <summary>
    /// Deterministic pruning: caps tracked characters at MaxTrackedChars
    /// and confusion pairs at MaxConfusionPairs per character.
    /// Removes least-attempted characters and least-frequent confusion pairs.
    /// Call periodically (e.g., on persistence save) to prevent unbounded growth.
    /// </summary>
    public void Prune()
    {
        // Cap tracked characters: remove least-attempted
        if (Records.Count > MaxTrackedChars)
        {
            var toRemove = Records
                .OrderBy(kvp => kvp.Value.Hits + kvp.Value.Misses)
                .Take(Records.Count - MaxTrackedChars)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
                Records.Remove(key);
        }

        // Cap confusion pairs per character: keep most frequent
        foreach (var record in Records.Values)
        {
            if (record.ConfusedWith.Count > MaxConfusionPairs)
            {
                var toKeep = record.ConfusedWith
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(MaxConfusionPairs)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                record.ConfusedWith.Clear();
                foreach (var kvp in toKeep)
                    record.ConfusedWith[kvp.Key] = kvp.Value;
            }
        }
    }

    private MistakeRecord GetOrCreate(char c)
    {
        if (!Records.TryGetValue(c, out var record))
        {
            record = new MistakeRecord();
            Records[c] = record;
        }
        return record;
    }

    /// <summary>
    /// Classifies a character into a symbol group for aggregation.
    /// </summary>
    public static SymbolGroup GetSymbolGroup(char c)
    {
        return c switch
        {
            '{' or '}' or '(' or ')' or '[' or ']' or '<' or '>' => SymbolGroup.Bracket,
            '\'' or '"' or '`' => SymbolGroup.Quote,
            '+' or '-' or '*' or '/' or '%' or '=' or '!' or '&' or '|' or '^' or '~' => SymbolGroup.Operator,
            ';' or ':' or ',' or '.' or '?' => SymbolGroup.Punctuation,
            ' ' or '\t' => SymbolGroup.Whitespace,
            '#' or '@' or '$' or '_' or '\\' => SymbolGroup.Special,
            _ when char.IsDigit(c) => SymbolGroup.Digit,
            _ when char.IsLetter(c) => SymbolGroup.Letter,
            _ => SymbolGroup.Other
        };
    }
}

/// <summary>
/// Tracks hit/miss frequency for a single character.
/// </summary>
public sealed class MistakeRecord
{
    /// <summary>
    /// Number of times this character was typed correctly.
    /// </summary>
    public int Hits { get; set; }

    /// <summary>
    /// Number of times this character was typed incorrectly.
    /// </summary>
    public int Misses { get; set; }

    /// <summary>
    /// When this character was last missed (UTC).
    /// </summary>
    public DateTime? LastMissedAt { get; set; }

    /// <summary>
    /// What the user typed instead (confusion pairs).
    /// Key = the incorrect char typed, Value = how many times.
    /// </summary>
    public Dictionary<char, int> ConfusedWith { get; set; } = new();

    /// <summary>
    /// Rolling window of recent attempts. true = hit, false = miss.
    /// Capped at <see cref="MistakeHeatmap.DefaultWindowSize"/> * 2 entries.
    /// Used to compute recent error rates that don't include ancient history.
    /// </summary>
    public List<bool> RecentAttempts { get; set; } = new();

    /// <summary>
    /// Pushes a hit/miss into the rolling window, capping at max capacity.
    /// </summary>
    internal void PushRecent(bool hit)
    {
        RecentAttempts.Add(hit);
        int cap = MistakeHeatmap.DefaultWindowSize * 2;
        if (RecentAttempts.Count > cap)
        {
            RecentAttempts.RemoveAt(0);
        }
    }
}

/// <summary>
/// Weakness info for a single character.
/// </summary>
public sealed class CharWeakness
{
    public char Character { get; set; }
    public double ErrorRate { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalMisses { get; set; }
    public SymbolGroup Group { get; set; }
    public char TopConfusion { get; set; }
}

/// <summary>
/// Weakness info for a group of characters (e.g., all brackets).
/// </summary>
public sealed class GroupWeakness
{
    public SymbolGroup Group { get; set; }
    public double ErrorRate { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalMisses { get; set; }
    public List<char> Characters { get; set; } = new();
}

/// <summary>
/// Categories for grouping characters in weakness analysis.
/// </summary>
public enum SymbolGroup
{
    Letter,
    Digit,
    Bracket,      // { } ( ) [ ] < >
    Quote,        // ' " `
    Operator,     // + - * / % = ! & | ^ ~
    Punctuation,  // ; : , . ?
    Whitespace,   // space, tab
    Special,      // # @ $ _ \
    Other
}
