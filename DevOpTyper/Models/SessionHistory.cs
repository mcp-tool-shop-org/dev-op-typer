namespace DevOpTyper.Models;

/// <summary>
/// Records a completed typing session for history tracking.
/// </summary>
public sealed class SessionRecord
{
    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Timestamp when session was completed (UTC).
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the snippet that was typed.
    /// </summary>
    public string SnippetId { get; set; } = "";

    /// <summary>
    /// Programming language of the snippet.
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>
    /// Title of the snippet.
    /// </summary>
    public string SnippetTitle { get; set; } = "";

    /// <summary>
    /// Words per minute achieved.
    /// </summary>
    public double Wpm { get; set; }

    /// <summary>
    /// Accuracy percentage (0-100).
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Total number of errors made.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Total characters typed.
    /// </summary>
    public int TotalChars { get; set; }

    /// <summary>
    /// Duration of the session in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Whether hardcore mode was enabled.
    /// </summary>
    public bool HardcoreMode { get; set; }

    /// <summary>
    /// Whether the session was completed perfectly (no errors).
    /// </summary>
    public bool IsPerfect => ErrorCount == 0;

    /// <summary>
    /// Creates a session record from a completed session.
    /// </summary>
    public static SessionRecord FromSession(
        string snippetId,
        string language,
        string snippetTitle,
        double wpm,
        double accuracy,
        int errorCount,
        int totalChars,
        TimeSpan duration,
        bool hardcoreMode)
    {
        return new SessionRecord
        {
            SnippetId = snippetId,
            Language = language,
            SnippetTitle = snippetTitle,
            Wpm = Math.Round(wpm, 1),
            Accuracy = Math.Round(accuracy, 1),
            ErrorCount = errorCount,
            TotalChars = totalChars,
            DurationSeconds = Math.Round(duration.TotalSeconds, 1),
            HardcoreMode = hardcoreMode
        };
    }
}

/// <summary>
/// Manages session history with statistics and filtering.
/// </summary>
public sealed class SessionHistory
{
    private const int MaxRecords = 500;

    /// <summary>
    /// All session records, most recent first.
    /// </summary>
    public List<SessionRecord> Records { get; set; } = new();

    /// <summary>
    /// Total number of sessions completed.
    /// </summary>
    public int TotalSessions => Records.Count;

    /// <summary>
    /// Add a new session record.
    /// </summary>
    public void AddRecord(SessionRecord record)
    {
        Records.Insert(0, record);

        // Trim old records if over limit
        if (Records.Count > MaxRecords)
        {
            Records.RemoveRange(MaxRecords, Records.Count - MaxRecords);
        }
    }

    /// <summary>
    /// Get records for a specific language.
    /// </summary>
    public IEnumerable<SessionRecord> GetByLanguage(string language)
    {
        return Records.Where(r => r.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get records for a specific time period.
    /// </summary>
    public IEnumerable<SessionRecord> GetByPeriod(DateTime from, DateTime to)
    {
        return Records.Where(r => r.CompletedAt >= from && r.CompletedAt <= to);
    }

    /// <summary>
    /// Get today's records.
    /// </summary>
    public IEnumerable<SessionRecord> GetToday()
    {
        var today = DateTime.UtcNow.Date;
        return GetByPeriod(today, today.AddDays(1));
    }

    /// <summary>
    /// Get this week's records.
    /// </summary>
    public IEnumerable<SessionRecord> GetThisWeek()
    {
        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        return GetByPeriod(startOfWeek, now);
    }

    /// <summary>
    /// Get lifetime statistics.
    /// </summary>
    public HistoryStatistics GetLifetimeStats()
    {
        return CalculateStats(Records);
    }

    /// <summary>
    /// Get statistics for a specific language.
    /// </summary>
    public HistoryStatistics GetLanguageStats(string language)
    {
        return CalculateStats(GetByLanguage(language).ToList());
    }

    /// <summary>
    /// Get statistics for today.
    /// </summary>
    public HistoryStatistics GetTodayStats()
    {
        return CalculateStats(GetToday().ToList());
    }

    /// <summary>
    /// Get personal best records.
    /// </summary>
    public PersonalBests GetPersonalBests()
    {
        if (Records.Count == 0)
        {
            return new PersonalBests();
        }

        return new PersonalBests
        {
            BestWpm = Records.Max(r => r.Wpm),
            BestAccuracy = Records.Max(r => r.Accuracy),
            LongestStreak = CalculateLongestPerfectStreak(),
            MostSessionsInDay = GetMostSessionsInDay(),
            FastestPerfect = Records.Where(r => r.IsPerfect).Select(r => r.Wpm).DefaultIfEmpty(0).Max()
        };
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void Clear()
    {
        Records.Clear();
    }

    private static HistoryStatistics CalculateStats(IList<SessionRecord> records)
    {
        if (records.Count == 0)
        {
            return new HistoryStatistics();
        }

        return new HistoryStatistics
        {
            TotalSessions = records.Count,
            TotalCharacters = records.Sum(r => r.TotalChars),
            TotalDurationMinutes = records.Sum(r => r.DurationSeconds) / 60.0,
            AverageWpm = records.Average(r => r.Wpm),
            AverageAccuracy = records.Average(r => r.Accuracy),
            PerfectSessions = records.Count(r => r.IsPerfect),
            HardcoreSessions = records.Count(r => r.HardcoreMode)
        };
    }

    private int CalculateLongestPerfectStreak()
    {
        int maxStreak = 0;
        int currentStreak = 0;

        foreach (var record in Records.OrderBy(r => r.CompletedAt))
        {
            if (record.IsPerfect)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }

    private int GetMostSessionsInDay()
    {
        if (Records.Count == 0) return 0;

        return Records
            .GroupBy(r => r.CompletedAt.Date)
            .Max(g => g.Count());
    }
}

/// <summary>
/// Aggregated statistics from session history.
/// </summary>
public sealed class HistoryStatistics
{
    public int TotalSessions { get; set; }
    public int TotalCharacters { get; set; }
    public double TotalDurationMinutes { get; set; }
    public double AverageWpm { get; set; }
    public double AverageAccuracy { get; set; }
    public int PerfectSessions { get; set; }
    public int HardcoreSessions { get; set; }

    public double PerfectRate => TotalSessions > 0 ? (double)PerfectSessions / TotalSessions * 100 : 0;
}

/// <summary>
/// Personal best records.
/// </summary>
public sealed class PersonalBests
{
    public double BestWpm { get; set; }
    public double BestAccuracy { get; set; }
    public int LongestStreak { get; set; }
    public int MostSessionsInDay { get; set; }
    public double FastestPerfect { get; set; }
}
