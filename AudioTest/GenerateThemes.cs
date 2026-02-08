using System;
using System.IO;

class GenerateThemes
{
    const int SAMPLE_RATE = 44100;
    static Random rng = new(42);

    static void Main2()
    {
        string sfxDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "DevOpTyper", "Assets", "Sounds", "Sfx"));

        Console.WriteLine($"SFX dir: {sfxDir}");

        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(sfxDir, "Membrane"));
        Directory.CreateDirectory(Path.Combine(sfxDir, "Thock"));
        Directory.CreateDirectory(Path.Combine(sfxDir, "Clicky"));

        for (int v = 1; v <= 8; v++)
        {
            double vary = 0.90 + rng.NextDouble() * 0.20; // 0.90 - 1.10

            // ================================================================
            // MEMBRANE: rubber dome keyboard — muted thud with housing resonance
            // Think: cheap office Dell keyboard, ~90ms
            // ================================================================
            {
                int samples = (int)(SAMPLE_RATE * 0.09);
                var data = new double[samples];
                double prev1 = 0, prev2 = 0, prev3 = 0, prev4 = 0;
                double bodyFreq = 90 + rng.NextDouble() * 30;   // deep case thud
                double plateFreq = 250 + rng.NextDouble() * 60; // plate resonance

                for (int i = 0; i < samples; i++)
                {
                    double t = (double)i / SAMPLE_RATE;
                    // Instant attack, two-stage decay (fast initial, slow tail)
                    double env = (Math.Exp(-t * 60) * 0.7 + Math.Exp(-t * 20) * 0.3) * vary;

                    // Broadband noise impact (filtered heavily)
                    double noise = rng.NextDouble() * 2 - 1;
                    double cutoff = 0.05 + 0.15 * Math.Exp(-t * 80); // filter opens on impact then closes
                    prev1 = prev1 * (1 - cutoff) + noise * cutoff;
                    prev2 = prev2 * (1 - cutoff) + prev1 * cutoff;
                    prev3 = prev3 * (1 - cutoff * 0.8) + prev2 * cutoff * 0.8;
                    prev4 = prev4 * (1 - cutoff * 0.6) + prev3 * cutoff * 0.6;

                    // Deep body thud
                    double body = Math.Sin(2 * Math.PI * bodyFreq * t) * Math.Exp(-t * 50) * 0.5;
                    // Plate flex
                    double plate = Math.Sin(2 * Math.PI * plateFreq * t) * Math.Exp(-t * 70) * 0.25;

                    data[i] = (prev4 * 0.6 + body + plate) * env;
                }
                ApplyFade(data, 4);
                WriteWav(Path.Combine(sfxDir, "Membrane", $"key_{v:D2}.wav"), Normalize(data, 0.80));
            }

            // ================================================================
            // THOCK: deep heavy bottom-out — lubed linear switch, GMK keycaps
            // Think: thick PBT/ABS keycap slamming into plate, pure low-end, ~110ms
            // ================================================================
            {
                int samples = (int)(SAMPLE_RATE * 0.11);
                var data = new double[samples];
                double bottomFreq = 150 + rng.NextDouble() * 50;   // deep bottom-out fundamental
                double caseFreq = 280 + rng.NextDouble() * 60;     // case resonance
                double plateFreq = 600 + rng.NextDouble() * 150;   // plate flex tone
                double np1 = 0, np2 = 0;

                for (int i = 0; i < samples; i++)
                {
                    double t = (double)i / SAMPLE_RATE;

                    // Phase 1: Impact — keycap hitting plate (0-4ms)
                    // Muffled thud, not bright — lubed switch = smooth, no rattle
                    double impact = 0;
                    if (t < 0.004)
                    {
                        double impProgress = t / 0.004;
                        double impEnv = (1.0 - impProgress) * (1.0 - impProgress);
                        double noise = rng.NextDouble() * 2 - 1;
                        np1 = np1 * 0.7 + noise * 0.3; // heavy LP = muffled
                        impact = np1 * impEnv * 0.8;
                    }

                    // Phase 2: Deep bottom-out thock (0-60ms) — THE sound
                    // Low fundamental with slow decay = heavy, satisfying
                    double thockEnv = Math.Exp(-t * 35);
                    double thock = Math.Sin(2 * Math.PI * bottomFreq * t) * thockEnv * 0.7;
                    // Second harmonic for warmth
                    thock += Math.Sin(2 * Math.PI * bottomFreq * 2.0 * t) * thockEnv * 0.15;

                    // Phase 3: Case resonance (5-80ms) — the keyboard body vibrating
                    double caseDelay = 1 - Math.Exp(-t * 400);
                    double caseEnv = Math.Exp(-t * 30) * caseDelay;
                    double caseRes = Math.Sin(2 * Math.PI * caseFreq * t) * caseEnv * 0.3;

                    // Phase 4: Plate flex (2-40ms) — subtle mid tone
                    double plateEnv = Math.Exp(-t * 70);
                    double plate = Math.Sin(2 * Math.PI * plateFreq * t) * plateEnv * 0.15;

                    // Phase 5: Low rumble tail (20-110ms) — foam dampened resonance
                    double rumbleNoise = rng.NextDouble() * 2 - 1;
                    np2 = np2 * 0.92 + rumbleNoise * 0.08; // very heavy LP
                    double rumbleEnv = Math.Exp(-t * 20) * (1 - Math.Exp(-t * 150));
                    double rumble = np2 * rumbleEnv * 0.12;

                    data[i] = (impact + thock + caseRes + plate + rumble) * vary;
                }
                ApplyFade(data, 5);
                WriteWav(Path.Combine(sfxDir, "Thock", $"key_{v:D2}.wav"), Normalize(data, 0.88));
            }

            // ================================================================
            // CLICKY: IBM Model M buckling spring — sharp snap + deep thock
            // Think: heavy keycap bottoming out on steel plate, spring buckling, ~100ms
            // ================================================================
            {
                int samples = (int)(SAMPLE_RATE * 0.10);
                var data = new double[samples];
                double buckleFreq = 1800 + rng.NextDouble() * 400;  // spring buckle tone
                double plateFreq = 3500 + rng.NextDouble() * 800;   // steel backplate ping
                double housingFreq = 400 + rng.NextDouble() * 100;  // case resonance
                double np = 0;

                for (int i = 0; i < samples; i++)
                {
                    double t = (double)i / SAMPLE_RATE;

                    // Phase 1: Buckling spring snap (0-3ms) — the characteristic "click"
                    // Sharp broadband burst, brighter than membrane
                    double snap = 0;
                    if (t < 0.003)
                    {
                        double snapProgress = t / 0.003;
                        double snapEnv = 1.0 - snapProgress;
                        double noise = rng.NextDouble() * 2 - 1;
                        np = np * 0.3 + noise * 0.7; // less filtering = brighter snap
                        snap = np * snapEnv * 1.0;
                    }

                    // Phase 2: Spring buckle resonance (0-20ms) — the "click" pitch
                    double buckleEnv = Math.Exp(-t * 180);
                    double buckle = Math.Sin(2 * Math.PI * buckleFreq * t) * buckleEnv * 0.5;
                    // Slight inharmonic overtone
                    buckle += Math.Sin(2 * Math.PI * buckleFreq * 2.7 * t) * buckleEnv * 0.12;

                    // Phase 3: Steel plate ping (1-50ms) — keycap bottoming on plate
                    double plateDelay = 1 - Math.Exp(-t * 600); // nearly instant
                    double plateEnv = Math.Exp(-t * 60) * plateDelay;
                    double plate = Math.Sin(2 * Math.PI * plateFreq * t) * plateEnv * 0.2;

                    // Phase 4: Keycap bottom-out thock (0-30ms)
                    double thockEnv = Math.Exp(-t * 100);
                    double thock = Math.Sin(2 * Math.PI * housingFreq * t) * thockEnv * 0.4;

                    // Phase 5: Housing echo (10-100ms) — the big heavy case resonating
                    double echoDelay = 1 - Math.Exp(-t * 200);
                    double echoEnv = Math.Exp(-t * 30) * echoDelay;
                    double echo = Math.Sin(2 * Math.PI * 150 * t) * echoEnv * 0.2;

                    data[i] = (snap + buckle + plate + thock + echo) * vary;
                }
                ApplyFade(data, 3);
                WriteWav(Path.Combine(sfxDir, "Clicky", $"key_{v:D2}.wav"), Normalize(data, 0.90));
            }
        }

        Console.WriteLine("\nAll 3 themes generated (8 variations each = 24 files)");
    }

    /// <summary>
    /// Normalize peak to target level and convert to 16-bit PCM.
    /// </summary>
    static short[] Normalize(double[] data, double targetPeak)
    {
        double maxAbs = 0;
        foreach (var s in data)
            if (Math.Abs(s) > maxAbs) maxAbs = Math.Abs(s);

        double scale = maxAbs > 0 ? targetPeak / maxAbs : 1.0;
        var result = new short[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            int val = (int)(data[i] * scale * 32767);
            result[i] = (short)Math.Max(-32767, Math.Min(32767, val));
        }
        return result;
    }

    /// <summary>
    /// Apply fade-out at end to avoid clicks.
    /// </summary>
    static void ApplyFade(double[] data, int fadeMs)
    {
        int fadeLen = (int)(SAMPLE_RATE * fadeMs / 1000.0);
        for (int i = data.Length - fadeLen; i < data.Length; i++)
        {
            double fade = (double)(data.Length - i) / fadeLen;
            data[i] *= fade;
        }
    }

    static void WriteWav(string path, short[] samples)
    {
        int dataBytes = samples.Length * 2;
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(SAMPLE_RATE);
        bw.Write(SAMPLE_RATE * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (var s in samples)
            bw.Write(s);

        short peak = 0;
        foreach (var s in samples)
            if (Math.Abs(s) > peak) peak = Math.Abs(s);
        double peakDb = peak > 0 ? 20 * Math.Log10((double)peak / 32767) : -99;

        string theme = Path.GetFileName(Path.GetDirectoryName(path))!;
        Console.WriteLine($"  {theme}/key_{Path.GetFileNameWithoutExtension(path).Split('_')[1]}: {samples.Length * 1000 / SAMPLE_RATE}ms, peak={peak} ({peakDb:F1} dB)");
    }
}
