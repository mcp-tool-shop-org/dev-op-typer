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
    /// Records a correctly typed character.
    /// </summary>
    public void RecordHit(char expected)
    {
        GetOrCreate(expected).Hits++;
    }

    /// <summary>
    /// Records an incorrectly typed character.
    /// </summary>
    public void RecordMiss(char expected, char? actual)
    {
        var record = GetOrCreate(expected);
        record.Misses++;
        record.LastMissedAt = DateTime.UtcNow;

        // Track what was typed instead (confusion pairs)
        if (actual.HasValue)
        {
            record.ConfusedWith.TryGetValue(actual.Value, out int count);
            record.ConfusedWith[actual.Value] = count + 1;
        }
    }

    /// <summary>
    /// Gets the error rate for a specific character (0.0 = perfect, 1.0 = always wrong).
    /// </summary>
    public double GetErrorRate(char c)
    {
        if (!Records.TryGetValue(c, out var record)) return 0;
        int total = record.Hits + record.Misses;
        return total > 0 ? (double)record.Misses / total : 0;
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
