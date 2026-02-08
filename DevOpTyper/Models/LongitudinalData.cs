namespace DevOpTyper.Models;

/// <summary>
/// Accumulates cross-session data for trend awareness.
/// This is raw signal storage — no interpretation, no scoring, no alerts.
///
/// v0.3.0: Data primitives only. Visualization and analysis come in Phase 3.
/// </summary>
public sealed class LongitudinalData
{
    /// <summary>
    /// Per-language rolling statistics. Key = language name (lowercase).
    /// </summary>
    public Dictionary<string, LanguageTrend> TrendsByLanguage { get; set; } = new();

    /// <summary>
    /// Timestamps of all completed sessions, newest first.
    /// Used for cadence analysis (session frequency, gaps, fatigue detection).
    /// Capped at 200 entries to bound storage.
    /// </summary>
    public List<DateTime> SessionTimestamps { get; set; } = new();

    /// <summary>
    /// Periodic snapshots of weakness state for tracking improvement over time.
    /// Captured at most once per day per language.
    /// </summary>
    public List<WeaknessSnapshot> WeaknessSnapshots { get; set; } = new();

    /// <summary>
    /// Records a completed session into longitudinal data.
    /// Call after saving the SessionRecord.
    /// </summary>
    public void RecordSession(SessionRecord record)
    {
        // Track timestamp
        SessionTimestamps.Insert(0, record.CompletedAt);
        if (SessionTimestamps.Count > 200)
        {
            SessionTimestamps.RemoveRange(200, SessionTimestamps.Count - 200);
        }

        // Update language trend
        var lang = record.Language?.ToLowerInvariant() ?? "unknown";
        if (!TrendsByLanguage.TryGetValue(lang, out var trend))
        {
            trend = new LanguageTrend();
            TrendsByLanguage[lang] = trend;
        }

        trend.RecordSession(record.Wpm, record.Accuracy, record.CompletedAt);
    }

    /// <summary>
    /// Captures a weakness snapshot if enough time has passed since the last one
    /// for this language (at most once per day).
    /// </summary>
    public void MaybeSnapshotWeakness(string language, MistakeHeatmap heatmap)
    {
        var lang = language?.ToLowerInvariant() ?? "unknown";
        var today = DateTime.UtcNow.Date;

        // Check if we already have a snapshot for this language today
        bool alreadySnapped = WeaknessSnapshots.Any(s =>
            s.Language == lang && s.CapturedAt.Date == today);

        if (alreadySnapped) return;

        var weakest = heatmap.GetWeakest(count: 10, minAttempts: 3);
        if (weakest.Count == 0) return;

        var snapshot = new WeaknessSnapshot
        {
            Language = lang,
            CapturedAt = DateTime.UtcNow,
            TopWeaknesses = weakest.Select(w => new WeaknessEntry
            {
                Character = w.Character,
                ErrorRate = Math.Round(w.ErrorRate, 3),
                TotalAttempts = w.TotalAttempts
            }).ToList()
        };

        WeaknessSnapshots.Add(snapshot);

        // Cap at 90 snapshots (~3 months of daily snapshots per language)
        if (WeaknessSnapshots.Count > 90)
        {
            WeaknessSnapshots.RemoveAt(0);
        }
    }
}

/// <summary>
/// Rolling statistics for a single language across sessions.
/// Stores enough data points to compute trends without keeping every session.
/// </summary>
public sealed class LanguageTrend
{
    /// <summary>
    /// Recent WPM values, newest first. Capped at 50.
    /// </summary>
    public List<double> RecentWpm { get; set; } = new();

    /// <summary>
    /// Recent accuracy values, newest first. Capped at 50.
    /// </summary>
    public List<double> RecentAccuracy { get; set; } = new();

    /// <summary>
    /// Total sessions completed in this language.
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// When this language was first practiced.
    /// </summary>
    public DateTime? FirstSessionAt { get; set; }

    /// <summary>
    /// When this language was last practiced.
    /// </summary>
    public DateTime? LastSessionAt { get; set; }

    /// <summary>
    /// Records a session's metrics into the trend.
    /// </summary>
    public void RecordSession(double wpm, double accuracy, DateTime completedAt)
    {
        RecentWpm.Insert(0, Math.Round(wpm, 1));
        RecentAccuracy.Insert(0, Math.Round(accuracy, 1));

        // Cap at 50 data points
        if (RecentWpm.Count > 50) RecentWpm.RemoveAt(RecentWpm.Count - 1);
        if (RecentAccuracy.Count > 50) RecentAccuracy.RemoveAt(RecentAccuracy.Count - 1);

        TotalSessions++;
        FirstSessionAt ??= completedAt;
        LastSessionAt = completedAt;
    }

    /// <summary>
    /// Gets the average WPM over the last N sessions (or all if fewer).
    /// Returns null if no data.
    /// </summary>
    public double? AverageWpm(int lastN = 10)
    {
        if (RecentWpm.Count == 0) return null;
        return RecentWpm.Take(lastN).Average();
    }

    /// <summary>
    /// Gets the average accuracy over the last N sessions (or all if fewer).
    /// Returns null if no data.
    /// </summary>
    public double? AverageAccuracy(int lastN = 10)
    {
        if (RecentAccuracy.Count == 0) return null;
        return RecentAccuracy.Take(lastN).Average();
    }
}

/// <summary>
/// A point-in-time snapshot of the user's weakest characters for a language.
/// </summary>
public sealed class WeaknessSnapshot
{
    public string Language { get; set; } = "";
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<WeaknessEntry> TopWeaknesses { get; set; } = new();
}

/// <summary>
/// A single character weakness at a point in time.
/// </summary>
public sealed class WeaknessEntry
{
    public char Character { get; set; }
    public double ErrorRate { get; set; }
    public int TotalAttempts { get; set; }
}
