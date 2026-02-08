using System;
using System.IO;

/// <summary>
/// Generates new candidate keyboard themes for audition.
/// Output goes to a temporary "NewThemes" folder for review.
/// Each theme has 8 key variants with subtle pitch/timing variation.
/// </summary>
class GenerateNewThemes
{
    const int SR = 44100;
    static Random rng = new(99);

    static void Main()
    {
        string outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "NewThemes"));

        Console.WriteLine($"Output dir: {outDir}");

        // ────────────────────────────────────────────────────────────
        // 1. TYPEWRITER — old mechanical typewriter hammer strike
        //    Metallic impact + paper thump + carriage vibration
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "Typewriter", (v, vary) =>
        {
            int samples = (int)(SR * 0.14);
            var data = new double[samples];
            double hammerFreq = 1200 + rng.NextDouble() * 300;
            double barFreq = 3200 + rng.NextDouble() * 600;
            double carriageFreq = 180 + rng.NextDouble() * 40;
            double np1 = 0, np2 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Hammer strike — sharp metallic transient (0-2ms)
                double strike = 0;
                if (t < 0.002)
                {
                    double n = rng.NextDouble() * 2 - 1;
                    np1 = np1 * 0.4 + n * 0.6;
                    strike = np1 * (1 - t / 0.002) * 1.2;
                }

                // Type bar ring — brief metallic resonance (0-15ms)
                double barEnv = Math.Exp(-t * 250);
                double bar = Math.Sin(2 * Math.PI * barFreq * t) * barEnv * 0.3;
                bar += Math.Sin(2 * Math.PI * barFreq * 1.6 * t) * barEnv * 0.1; // inharmonic

                // Hammer thud on platen — paper/rubber absorbing impact (0-30ms)
                double thudEnv = Math.Exp(-t * 80);
                double thud = Math.Sin(2 * Math.PI * hammerFreq * 0.15 * t) * thudEnv * 0.5;

                // Carriage vibration — low rumble from the mechanism (5-140ms)
                double carriageDelay = 1 - Math.Exp(-t * 300);
                double carriageEnv = Math.Exp(-t * 18) * carriageDelay;
                double n2 = rng.NextDouble() * 2 - 1;
                np2 = np2 * 0.93 + n2 * 0.07;
                double carriage = (np2 * 0.3 + Math.Sin(2 * Math.PI * carriageFreq * t) * 0.7)
                                  * carriageEnv * 0.2;

                // Spring return click — tiny secondary transient at ~8ms
                double springClick = 0;
                double springT = t - 0.008;
                if (springT > 0 && springT < 0.001)
                {
                    springClick = (rng.NextDouble() * 2 - 1) * (1 - springT / 0.001) * 0.3;
                }

                data[i] = (strike + bar + thud + carriage + springClick) * vary;
            }
            ApplyFade(data, 6);
            return (data, 0.82);
        });

        // ────────────────────────────────────────────────────────────
        // 2. SOFT TOUCH — quiet laptop keyboard, dampened chiclet keys
        //    Very short, soft, almost silent — gentle taps
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "SoftTouch", (v, vary) =>
        {
            int samples = (int)(SR * 0.06);
            var data = new double[samples];
            double np1 = 0, np2 = 0, np3 = 0;
            double bodyFreq = 200 + rng.NextDouble() * 80;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Very gentle tap — heavily filtered noise burst
                double tapEnv = Math.Exp(-t * 120);
                double n = rng.NextDouble() * 2 - 1;
                double cutoff = 0.03 + 0.08 * Math.Exp(-t * 200);
                np1 = np1 * (1 - cutoff) + n * cutoff;
                np2 = np2 * (1 - cutoff) + np1 * cutoff;
                np3 = np3 * (1 - cutoff * 0.7) + np2 * cutoff * 0.7;
                double tap = np3 * tapEnv * 0.8;

                // Tiny body thump — scissor mechanism flex
                double bodyEnv = Math.Exp(-t * 150);
                double body = Math.Sin(2 * Math.PI * bodyFreq * t) * bodyEnv * 0.25;

                // Keycap bottom-out — micro plastic tick
                double tickEnv = Math.Exp(-t * 300);
                double tick = Math.Sin(2 * Math.PI * 2500 * t) * tickEnv * 0.08;

                data[i] = (tap + body + tick) * vary;
            }
            ApplyFade(data, 3);
            return (data, 0.70);
        });

        // ────────────────────────────────────────────────────────────
        // 3. CHERRY RED — smooth linear switch, medium pitch, no click
        //    Clean bottom-out, slight spring ping, moderate depth
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "CherryRed", (v, vary) =>
        {
            int samples = (int)(SR * 0.10);
            var data = new double[samples];
            double bottomFreq = 200 + rng.NextDouble() * 40;
            double springFreq = 4200 + rng.NextDouble() * 800;
            double plateFreq = 900 + rng.NextDouble() * 200;
            double np1 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Bottom-out impact — clean, no rattle
                double impactEnv = Math.Exp(-t * 90);
                double n = rng.NextDouble() * 2 - 1;
                np1 = np1 * 0.6 + n * 0.4;
                double impact = np1 * Math.Max(0, Math.Exp(-t * 500)) * 0.5;

                // Smooth bottom — low-mid thud, the main body of the sound
                double bottomEnv = Math.Exp(-t * 45);
                double bottom = Math.Sin(2 * Math.PI * bottomFreq * t) * bottomEnv * 0.55;
                bottom += Math.Sin(2 * Math.PI * bottomFreq * 2 * t) * bottomEnv * 0.12;

                // Spring ping — subtle high metallic ring
                double springEnv = Math.Exp(-t * 200);
                double spring = Math.Sin(2 * Math.PI * springFreq * t) * springEnv * 0.06;

                // Plate resonance — mid tone
                double plateEnv = Math.Exp(-t * 60);
                double plate = Math.Sin(2 * Math.PI * plateFreq * t) * plateEnv * 0.15;

                data[i] = (impact + bottom + spring + plate) * vary;
            }
            ApplyFade(data, 4);
            return (data, 0.84);
        });

        // ────────────────────────────────────────────────────────────
        // 4. TOPRE — electrostatic capacitive switch (HHKB / Realforce)
        //    Characteristic "thock" with rubber dome cushion + spring pop
        //    Deeper and more muffled than MX, with distinctive "pop" on upstroke
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "Topre", (v, vary) =>
        {
            int samples = (int)(SR * 0.12);
            var data = new double[samples];
            double domeFreq = 120 + rng.NextDouble() * 30;
            double popFreq = 1600 + rng.NextDouble() * 300;
            double housingFreq = 350 + rng.NextDouble() * 80;
            double np1 = 0, np2 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Dome collapse — the soft but definitive "thock" (0-5ms)
                double collapseEnv = 0;
                if (t < 0.005)
                {
                    double n = rng.NextDouble() * 2 - 1;
                    np1 = np1 * 0.55 + n * 0.45;
                    collapseEnv = (1 - t / 0.005);
                }
                double collapse = np1 * collapseEnv * 0.7;

                // Rubber dome bottom — deep, muffled fundamental
                double domeEnv = Math.Exp(-t * 55);
                double dome = Math.Sin(2 * Math.PI * domeFreq * t) * domeEnv * 0.6;
                dome += Math.Sin(2 * Math.PI * domeFreq * 2.3 * t) * domeEnv * 0.12; // slight inharmonic

                // Housing resonance — mid-range body
                double houseDelay = 1 - Math.Exp(-t * 500);
                double houseEnv = Math.Exp(-t * 40) * houseDelay;
                double house = Math.Sin(2 * Math.PI * housingFreq * t) * houseEnv * 0.25;

                // Spring "pop" — comes at ~15ms, quick and bright
                double popT = t - 0.015;
                double pop = 0;
                if (popT > 0)
                {
                    double popEnv = Math.Exp(-popT * 350);
                    pop = Math.Sin(2 * Math.PI * popFreq * popT) * popEnv * 0.15;
                }

                // Low rumble tail — dampened by thick PBT keycap
                double n2 = rng.NextDouble() * 2 - 1;
                np2 = np2 * 0.94 + n2 * 0.06;
                double rumbleEnv = Math.Exp(-t * 22) * (1 - Math.Exp(-t * 200));
                double rumble = np2 * rumbleEnv * 0.08;

                data[i] = (collapse + dome + house + pop + rumble) * vary;
            }
            ApplyFade(data, 5);
            return (data, 0.85);
        });

        // ────────────────────────────────────────────────────────────
        // 5. ALPS CREAM — vintage Alps SKCM Cream Damped
        //    Smooth linear with distinctive dampened bottom-out
        //    Warm, slightly metallic, classic vintage feel
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "AlpsCream", (v, vary) =>
        {
            int samples = (int)(SR * 0.11);
            var data = new double[samples];
            double stemFreq = 260 + rng.NextDouble() * 50;
            double metalFreq = 2800 + rng.NextDouble() * 500;
            double caseFreq = 160 + rng.NextDouble() * 40;
            double np1 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Stem impact — slightly sharper than Topre (0-3ms)
                double impactEnv = 0;
                if (t < 0.003)
                {
                    double n = rng.NextDouble() * 2 - 1;
                    np1 = np1 * 0.45 + n * 0.55;
                    impactEnv = (1 - t / 0.003);
                }
                double impact = np1 * impactEnv * 0.6;

                // Stem-on-housing bottom — warm, mid-focused
                double stemEnv = Math.Exp(-t * 50);
                double stem = Math.Sin(2 * Math.PI * stemFreq * t) * stemEnv * 0.50;
                stem += Math.Sin(2 * Math.PI * stemFreq * 1.5 * t) * stemEnv * 0.15; // fifth harmonic

                // Subtle metallic leaf spring — vintage character
                double metalEnv = Math.Exp(-t * 180);
                double metal = Math.Sin(2 * Math.PI * metalFreq * t) * metalEnv * 0.10;
                metal += Math.Sin(2 * Math.PI * metalFreq * 0.7 * t) * metalEnv * 0.05; // sub-harmonic

                // Case resonance — warm aluminum vintage case
                double caseDelay = 1 - Math.Exp(-t * 400);
                double caseEnv = Math.Exp(-t * 30) * caseDelay;
                double caseRes = Math.Sin(2 * Math.PI * caseFreq * t) * caseEnv * 0.20;

                // Dampener pad absorb — very fast high-cut after impact
                double dampEnv = Math.Exp(-t * 100);
                double damp = Math.Sin(2 * Math.PI * 80 * t) * dampEnv * 0.12;

                data[i] = (impact + stem + metal + caseRes + damp) * vary;
            }
            ApplyFade(data, 5);
            return (data, 0.82);
        });

        // ────────────────────────────────────────────────────────────
        // 6. BUBBLE — playful, soft, almost game-like pop
        //    Short sine burst with quick pitch drop, toy-like
        //    Fun and satisfying, not trying to be realistic
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "Bubble", (v, vary) =>
        {
            int samples = (int)(SR * 0.08);
            var data = new double[samples];
            double startFreq = 800 + rng.NextDouble() * 200;
            double endFreq = 200 + rng.NextDouble() * 60;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Pitch drops quickly — bubble pop effect
                double freqT = startFreq + (endFreq - startFreq) * (1 - Math.Exp(-t * 80));
                double phase = 0;
                // Integrate frequency for phase
                for (int j = 0; j <= i; j++)
                {
                    double tj = (double)j / SR;
                    double fj = startFreq + (endFreq - startFreq) * (1 - Math.Exp(-tj * 80));
                    phase += fj / SR;
                }

                // Smooth envelope — quick attack, medium decay
                double env = Math.Sin(Math.PI * t / (samples / (double)SR)) * Math.Exp(-t * 30);

                double pop = Math.Sin(2 * Math.PI * phase) * env * 0.7;
                // Add subtle harmonic for body
                pop += Math.Sin(4 * Math.PI * phase) * env * 0.15;

                data[i] = pop * vary;
            }
            ApplyFade(data, 4);
            return (data, 0.78);
        });

        // ────────────────────────────────────────────────────────────
        // 7. RAINDROP — clean, pure, almost musical
        //    High-pitched tap with resonant decay, like water dripping
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "Raindrop", (v, vary) =>
        {
            int samples = (int)(SR * 0.15);
            var data = new double[samples];
            // Each key is a different note in a pentatonic scale
            double[] notes = { 523.25, 587.33, 659.25, 783.99, 880.00, 1046.5, 1174.7, 1318.5 };
            double freq = notes[v - 1] * (0.98 + rng.NextDouble() * 0.04);
            double np1 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Water drop impact — tiny noise burst
                double dropEnv = Math.Exp(-t * 600);
                double n = rng.NextDouble() * 2 - 1;
                np1 = np1 * 0.3 + n * 0.7;
                double drop = np1 * dropEnv * 0.2;

                // Resonant tone — pure sine with long-ish decay
                double toneEnv = Math.Exp(-t * 20);
                double tone = Math.Sin(2 * Math.PI * freq * t) * toneEnv * 0.6;
                // Octave shimmer
                tone += Math.Sin(2 * Math.PI * freq * 2 * t) * toneEnv * Math.Exp(-t * 40) * 0.15;
                // Fifth for warmth
                tone += Math.Sin(2 * Math.PI * freq * 1.5 * t) * toneEnv * Math.Exp(-t * 30) * 0.08;

                // Ripple — subtle wobble in amplitude
                double ripple = 1.0 + 0.15 * Math.Sin(2 * Math.PI * 12 * t) * Math.Exp(-t * 10);

                data[i] = (drop + tone) * ripple * vary;
            }
            ApplyFade(data, 8);
            return (data, 0.72);
        });

        // ────────────────────────────────────────────────────────────
        // 8. CREAMY — ultra-smooth, buttery, satisfying thock
        //    Like a perfectly lubed switch with thick PBT keycaps
        //    Round, deep, zero harshness
        // ────────────────────────────────────────────────────────────
        GenerateTheme(outDir, "Creamy", (v, vary) =>
        {
            int samples = (int)(SR * 0.13);
            var data = new double[samples];
            double fund = 130 + rng.NextDouble() * 25;
            double np1 = 0, np2 = 0;

            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SR;

                // Ultra-soft impact — almost no click, just weight
                double n = rng.NextDouble() * 2 - 1;
                np1 = np1 * 0.8 + n * 0.2; // very heavy filtering
                np2 = np2 * 0.85 + np1 * 0.15;
                double impactEnv = Math.Exp(-t * 200);
                double impact = np2 * impactEnv * 0.3;

                // Deep, round fundamental — THE creamy thock
                double fundEnv = Math.Exp(-t * 28);
                double thock = Math.Sin(2 * Math.PI * fund * t) * fundEnv * 0.65;
                // Warm second harmonic
                thock += Math.Sin(2 * Math.PI * fund * 2 * t) * fundEnv * Math.Exp(-t * 15) * 0.20;
                // Subtle third for character
                thock += Math.Sin(2 * Math.PI * fund * 3 * t) * fundEnv * Math.Exp(-t * 50) * 0.08;

                // Foam dampened tail — very low, very slow decay
                double foamEnv = Math.Exp(-t * 12) * (1 - Math.Exp(-t * 100));
                double foam = Math.Sin(2 * Math.PI * fund * 0.5 * t) * foamEnv * 0.15;

                data[i] = (impact + thock + foam) * vary;
            }
            ApplyFade(data, 6);
            return (data, 0.86);
        });

        Console.WriteLine($"\nAll new themes generated in: {outDir}");
    }

    // ════════════════════════════════════════════════════════════════
    // Helper: generates 8 key variants for a named theme
    // ════════════════════════════════════════════════════════════════
    static void GenerateTheme(string outDir, string name, Func<int, double, (double[] data, double peak)> generate)
    {
        string themeDir = Path.Combine(outDir, name);
        Directory.CreateDirectory(themeDir);
        Console.WriteLine($"\n{name}:");

        for (int v = 1; v <= 8; v++)
        {
            double vary = 0.90 + rng.NextDouble() * 0.20;
            var (data, targetPeak) = generate(v, vary);
            WriteWav(Path.Combine(themeDir, $"key_{v:D2}.wav"), Normalize(data, targetPeak));
        }
    }

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

    static void ApplyFade(double[] data, int fadeMs)
    {
        int fadeLen = (int)(SR * fadeMs / 1000.0);
        fadeLen = Math.Min(fadeLen, data.Length);
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
        bw.Write(SR);
        bw.Write(SR * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (var s in samples) bw.Write(s);

        short peak = 0;
        foreach (var s in samples)
            if (Math.Abs(s) > peak) peak = Math.Abs(s);
        double peakDb = peak > 0 ? 20 * Math.Log10((double)peak / 32767) : -99;
        double durMs = (double)samples.Length * 1000 / SR;
        Console.WriteLine($"  key_{Path.GetFileNameWithoutExtension(path).Split('_')[1]}: {durMs:F0}ms, peak={peak} ({peakDb:F1} dB)");
    }
}
