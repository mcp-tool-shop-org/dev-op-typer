using System.Diagnostics;

namespace DevOpTyper.Models;

public sealed class SessionState
{
    private readonly Stopwatch _sw = new();

    public Profile Profile { get; set; } = new();

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    public int ErrorCount { get; private set; }

    public double LiveWpm { get; private set; }
    public double LiveAccuracy { get; private set; } = 100.0;

    public int XpEarned { get; private set; }

    /// <summary>
    /// Snippet difficulty (1-5), used for XP multiplier.
    /// </summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>
    /// Minimum accuracy to earn XP (0-100). Below this, XP = 0.
    /// </summary>
    public double AccuracyFloor { get; set; } = 70.0;

    /// <summary>
    /// Number of times the same snippet has been typed recently.
    /// Used for diminishing returns. 0 = first time, 1 = second, etc.
    /// </summary>
    public int RepeatCount { get; set; } = 0;

    private string _target = "";

    /// <summary>
    /// Gets the elapsed time of the current/last session.
    /// </summary>
    public TimeSpan Elapsed => _sw.Elapsed;

    public void Start(string target)
    {
        _target = target ?? "";
        IsRunning = true;
        IsComplete = false;
        ErrorCount = 0;
        LiveWpm = 0;
        LiveAccuracy = 100;
        XpEarned = 0;

        _sw.Reset();
        _sw.Start();
    }

    public void Update(string typed, bool hardcoreMode)
    {
        if (!IsRunning) return;
        typed ??= "";

        // Hardcore mode: don't allow deleting past first error (basic implementation)
        // (Better version: intercept TextChanging to enforce. This starter keeps it simple.)

        // Compare char-by-char
        int minLen = Math.Min(typed.Length, _target.Length);
        int errors = 0;
        for (int i = 0; i < minLen; i++)
        {
            if (typed[i] != _target[i]) errors++;
        }
        // Extra chars beyond target count as errors
        errors += Math.Max(0, typed.Length - _target.Length);

        ErrorCount = errors;

        // Accuracy
        int denom = Math.Max(1, Math.Max(_target.Length, typed.Length));
        LiveAccuracy = 100.0 * (1.0 - (double)errors / denom);
        if (LiveAccuracy < 0) LiveAccuracy = 0;

        // WPM (treat 5 chars = 1 word)
        double minutes = Math.Max(1e-6, _sw.Elapsed.TotalMinutes);
        LiveWpm = (typed.Length / 5.0) / minutes;

        // XP formula (v0.2.0): accuracy floor + difficulty mult + speed curve + diminishing returns
        //
        // 1. Accuracy floor: below threshold → 0 XP
        // 2. Base XP = WPM * accuracy factor (with soft cap above 80 WPM)
        // 3. Difficulty multiplier: D1=0.6x, D2=0.8x, D3=1.0x, D4=1.3x, D5=1.6x
        // 4. Diminishing returns: -50% 2nd time, -75% 3rd+
        // 5. Completion bonus: +25 XP for finishing
        //
        if (LiveAccuracy < AccuracyFloor)
        {
            XpEarned = 0; // Below accuracy floor — no XP
        }
        else
        {
            // Speed curve: linear up to 80 WPM, then diminishing returns
            double speedFactor = LiveWpm <= 80
                ? LiveWpm
                : 80 + (LiveWpm - 80) * 0.3; // Soft cap above 80

            // Base XP
            double baseXp = speedFactor * (LiveAccuracy / 100.0) * 0.8;

            // Difficulty multiplier
            double diffMult = Difficulty switch
            {
                1 => 0.6,
                2 => 0.8,
                3 => 1.0,
                4 => 1.3,
                5 => 1.6,
                _ => 1.0
            };

            // Diminishing returns for repeats
            double repeatMult = RepeatCount switch
            {
                0 => 1.0,   // First time: full XP
                1 => 0.5,   // Second time: half
                _ => 0.25   // Third+: quarter
            };

            XpEarned = (int)Math.Round(baseXp * diffMult * repeatMult);
        }

        if (typed.Length >= _target.Length && errors == 0 && typed == _target)
        {
            IsComplete = true;
            if (LiveAccuracy >= AccuracyFloor)
            {
                XpEarned += 25; // Completion bonus (only if above accuracy floor)
            }
            IsRunning = false;
            _sw.Stop();
        }
        else
        {
            IsComplete = false;
        }
    }

    /// <summary>
    /// Cancels the current session without recording results.
    /// </summary>
    public void Cancel()
    {
        if (!IsRunning) return;
        IsRunning = false;
        IsComplete = false;
        _sw.Stop();
        _target = "";
    }
}
