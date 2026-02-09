using System;
using System.IO;

/// <summary>
/// Generates additional Zen drone/pad ambient tracks.
/// All warm, sustained, tonal — in the style of om_drone and ethereal_pad.
/// </summary>
class GenerateZenDrones
{
    const int SR = 44100;

    static void Main()
    {
        string zenDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "DevOpTyper", "Assets", "Sounds", "Ambient", "Zen"));

        Directory.CreateDirectory(zenDir);
        Console.WriteLine($"Zen dir: {zenDir}");

        // 1. Deep Earth Drone — sub-bass fundamental with warm harmonics
        // Like om_drone but lower and wider, feels like the earth humming
        {
            int dur = 53;
            var data = new double[SR * dur];
            double fund = 55; // A1 — very low
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double breathe = 0.5 + 0.25 * Math.Sin(2 * Math.PI * t / 17.3)
                                + 0.15 * Math.Sin(2 * Math.PI * t / 11.7)
                                + 0.1 * Math.Sin(2 * Math.PI * t / 29.3);

                double drone = Math.Sin(2 * Math.PI * fund * t) * 0.35;
                drone += Math.Sin(2 * Math.PI * (fund + 0.3) * t) * 0.12;       // beating
                drone += Math.Sin(2 * Math.PI * fund * 2 * t) * 0.15;           // octave
                drone += Math.Sin(2 * Math.PI * fund * 3 * t) * 0.08;           // 12th
                drone += Math.Sin(2 * Math.PI * fund * 4 * t) * 0.04;           // 2 octaves
                drone += Math.Sin(2 * Math.PI * (fund * 2 + 0.5) * t) * 0.06;   // octave beating
                // Sub undertone
                drone += Math.Sin(2 * Math.PI * fund * 0.5 * t) * 0.10;

                data[i] = drone * breathe;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(zenDir, "zen_deep_earth_drone.wav"), Normalize(data, 0.42));
        }

        // 2. Crystal Harmonics — higher, shimmering, glass-like overtones
        // Like singing bowls but continuous, with beating partials
        {
            int dur = 59;
            var data = new double[SR * dur];
            double fund = 330; // E4
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double breathe = 0.5 + 0.2 * Math.Sin(2 * Math.PI * t / 19.1)
                                + 0.15 * Math.Sin(2 * Math.PI * t / 13.3)
                                + 0.15 * Math.Sin(2 * Math.PI * t / 31.7);

                // Pure tones with slow beating pairs
                double crystal = Math.Sin(2 * Math.PI * fund * t) * 0.20;
                crystal += Math.Sin(2 * Math.PI * (fund + 0.8) * t) * 0.12;     // beating
                crystal += Math.Sin(2 * Math.PI * fund * 2 * t) * 0.10;
                crystal += Math.Sin(2 * Math.PI * (fund * 2 + 1.2) * t) * 0.06; // beating octave
                crystal += Math.Sin(2 * Math.PI * fund * 3 * t) * 0.05;
                crystal += Math.Sin(2 * Math.PI * fund * 1.5 * t) * 0.08;       // fifth
                crystal += Math.Sin(2 * Math.PI * (fund * 1.5 + 0.6) * t) * 0.04; // beating fifth
                // Low anchor
                crystal += Math.Sin(2 * Math.PI * fund * 0.5 * t) * 0.06;

                data[i] = crystal * breathe;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(zenDir, "zen_crystal_harmonics.wav"), Normalize(data, 0.40));
        }

        // 3. Warm Fifths — stacked perfect fifths with gentle detuning
        // Like ethereal_pad but richer, more enveloping
        {
            int dur = 61;
            var data = new double[SR * dur];
            double fund = 82.4; // E2
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double breathe = 0.45 + 0.25 * Math.Sin(2 * Math.PI * t / 15.7)
                                + 0.2 * Math.Sin(2 * Math.PI * t / 23.3)
                                + 0.1 * Math.Sin(2 * Math.PI * t / 7.1);

                // Stack: root, fifth, octave, octave+fifth, 2 octaves
                double pad = Math.Sin(2 * Math.PI * fund * t) * 0.25;
                pad += Math.Sin(2 * Math.PI * (fund + 0.4) * t) * 0.10;        // detune
                pad += Math.Sin(2 * Math.PI * fund * 1.5 * t) * 0.20;          // P5
                pad += Math.Sin(2 * Math.PI * (fund * 1.5 + 0.3) * t) * 0.08;  // detune P5
                pad += Math.Sin(2 * Math.PI * fund * 2 * t) * 0.15;            // 8va
                pad += Math.Sin(2 * Math.PI * fund * 3 * t) * 0.10;            // 8va + P5
                pad += Math.Sin(2 * Math.PI * (fund * 3 + 0.7) * t) * 0.04;    // detune
                pad += Math.Sin(2 * Math.PI * fund * 4 * t) * 0.06;            // 2x 8va

                data[i] = pad * breathe;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(zenDir, "zen_warm_fifths.wav"), Normalize(data, 0.42));
        }

        // 4. Theta Pulse — binaural-style low drone with 6 Hz amplitude modulation
        // Deep focus tone, rhythmic pulsing at theta brainwave frequency
        {
            int dur = 47;
            var data = new double[SR * dur];
            double fund = 90; // low tone
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;

                // Slow swell underneath the pulse
                double swell = 0.6 + 0.2 * Math.Sin(2 * Math.PI * t / 19.7)
                              + 0.15 * Math.Sin(2 * Math.PI * t / 13.1);

                // 6 Hz theta modulation — gentle, not abrupt
                double theta = 0.65 + 0.35 * Math.Sin(2 * Math.PI * 6.0 * t);

                double drone = Math.Sin(2 * Math.PI * fund * t) * 0.30;
                drone += Math.Sin(2 * Math.PI * (fund + 6) * t) * 0.15;   // binaural offset
                drone += Math.Sin(2 * Math.PI * fund * 2 * t) * 0.10;
                drone += Math.Sin(2 * Math.PI * fund * 0.5 * t) * 0.08;   // sub

                data[i] = drone * theta * swell;
            }
            ApplyLoopFade(data, 4.0);
            WriteWav(Path.Combine(zenDir, "zen_theta_pulse.wav"), Normalize(data, 0.40));
        }

        // 5. Celestial Wash — wide, airy, high-register shimmer
        // Like distant choir or space ambience
        {
            int dur = 53;
            var data = new double[SR * dur];
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double breathe = 0.4 + 0.25 * Math.Sin(2 * Math.PI * t / 21.7)
                                + 0.2 * Math.Sin(2 * Math.PI * t / 14.3)
                                + 0.15 * Math.Sin(2 * Math.PI * t / 37.1);

                // High, spacious tones with close beating
                double wash = Math.Sin(2 * Math.PI * 440 * t) * 0.12;
                wash += Math.Sin(2 * Math.PI * 441.5 * t) * 0.10;          // beating A
                wash += Math.Sin(2 * Math.PI * 660 * t) * 0.08;            // E5
                wash += Math.Sin(2 * Math.PI * 661.2 * t) * 0.06;          // beating E
                wash += Math.Sin(2 * Math.PI * 880 * t) * 0.05;            // A5
                wash += Math.Sin(2 * Math.PI * 550 * t) * 0.06;            // C#5 (major third)
                wash += Math.Sin(2 * Math.PI * 551 * t) * 0.04;            // beating
                // Low anchor far below
                wash += Math.Sin(2 * Math.PI * 110 * t) * 0.08;
                wash += Math.Sin(2 * Math.PI * 110.3 * t) * 0.04;          // beating anchor

                data[i] = wash * breathe;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(zenDir, "zen_celestial_wash.wav"), Normalize(data, 0.38));
        }

        Console.WriteLine("\nAll Zen drone variants generated!");
    }

    static void ApplyLoopFade(double[] data, double fadeSeconds)
    {
        int fadeSamples = (int)(SR * fadeSeconds);
        fadeSamples = Math.Min(fadeSamples, data.Length / 4);
        for (int i = 0; i < fadeSamples; i++)
        {
            double fade = (double)i / fadeSamples;
            fade = fade * fade;
            data[i] *= fade;
        }
        for (int i = 0; i < fadeSamples; i++)
        {
            int idx = data.Length - 1 - i;
            double fade = (double)i / fadeSamples;
            fade = fade * fade;
            data[idx] *= fade;
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
        double durSec = (double)samples.Length / SR;
        Console.WriteLine($"  {Path.GetFileNameWithoutExtension(path)}: {durSec:F1}s, peak={peak} ({peakDb:F1} dB)");
    }
}
