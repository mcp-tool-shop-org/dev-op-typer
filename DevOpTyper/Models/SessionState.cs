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

    private string _target = "";

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

        // XP: reward speed + accuracy + completion bonus
        XpEarned = (int)Math.Round((LiveWpm * (LiveAccuracy / 100.0)) * 0.8);
        if (typed.Length >= _target.Length && errors == 0 && typed == _target)
        {
            IsComplete = true;
            XpEarned += 25;
            IsRunning = false;
            _sw.Stop();
        }
        else
        {
            IsComplete = false;
        }
    }
}
