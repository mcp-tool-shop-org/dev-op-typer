using System;
using System.IO;

/// <summary>
/// Generates synthesized ambient soundscape tracks for DevOpTyper.
/// Each track is 30-60 seconds, 44100 Hz, 16-bit mono with fade in/out.
/// Organized into soundscape subdirectories under Assets/Sounds/Ambient/.
/// </summary>
class GenerateAmbient
{
    const int SR = 44100;
    static Random rng = new(777);

    static void Main3a()
    {
        string ambDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "DevOpTyper", "Assets", "Sounds", "Ambient"));

        Console.WriteLine($"Ambient dir: {ambDir}");

        // Delete old Default files
        var oldDefault = Path.Combine(ambDir, "Default");
        if (Directory.Exists(oldDefault))
        {
            foreach (var f in Directory.GetFiles(oldDefault, "*.wav"))
            {
                File.Delete(f);
                Console.WriteLine($"  Deleted: {Path.GetFileName(f)}");
            }
            Directory.Delete(oldDefault);
        }

        // Generate all soundscapes
        GenerateOcean(ambDir);
        GenerateForest(ambDir);
        GenerateZen(ambDir);
        GenerateMountain(ambDir);
        GenerateRain(ambDir);
        GenerateNight(ambDir);

        Console.WriteLine("\nAll ambient soundscapes generated!");
    }

    // ================================================================
    // OCEAN — waves, underwater, shore
    // ================================================================
    static void GenerateOcean(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Ocean");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Ocean ===");

        // 1. Gentle ocean waves — slow surging white noise shaped like waves
        {
            int dur = 47; // prime-ish
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                // Wave cycle: slow surge every 6-9 seconds
                double wave1 = 0.5 + 0.5 * Math.Sin(2 * Math.PI * t / 7.3);
                double wave2 = 0.5 + 0.5 * Math.Sin(2 * Math.PI * t / 11.1 + 1.2);
                double waveEnv = wave1 * 0.6 + wave2 * 0.4;

                double noise = rng.NextDouble() * 2 - 1;
                // Low-pass filter chain for deep ocean rumble
                double cutoff = 0.02 + 0.04 * waveEnv;
                np1 = np1 * (1 - cutoff) + noise * cutoff;
                np2 = np2 * (1 - cutoff * 0.7) + np1 * cutoff * 0.7;
                np3 = np3 * (1 - cutoff * 0.5) + np2 * cutoff * 0.5;
                np4 = np4 * (1 - cutoff * 0.3) + np3 * cutoff * 0.3;

                // Mix filtered noise (wash) with wave-shaped volume
                data[i] = np4 * waveEnv * 0.7 + np2 * waveEnv * 0.3;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "ocean_gentle_waves.wav"), Normalize(data, 0.55));
        }

        // 2. Deep underwater — muted resonance, hydrophone character
        {
            int dur = 41;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Very heavy LP for muted underwater feel
                double cut = 0.008 + 0.005 * Math.Sin(2 * Math.PI * t / 13.7);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;

                // Deep resonant hum
                double hum = Math.Sin(2 * Math.PI * 55 * t) * 0.05;
                double hum2 = Math.Sin(2 * Math.PI * 82.5 * t) * 0.03;

                // Occasional bubble-like bursts
                double bubble = 0;
                double bubbleCycle = (t % 4.7) / 4.7;
                if (bubbleCycle > 0.92)
                {
                    double bEnv = (bubbleCycle - 0.92) / 0.08;
                    bEnv = Math.Sin(bEnv * Math.PI);
                    bubble = Math.Sin(2 * Math.PI * (800 - 400 * bEnv) * t) * bEnv * 0.03;
                }

                data[i] = np6 * 0.8 + hum + hum2 + bubble;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "ocean_deep_underwater.wav"), Normalize(data, 0.50));
        }

        // 3. Shore with pebbles — higher freq "shuffling" water retreat
        {
            int dur = 43;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Wave advance/retreat cycle
                double cycle = (t % 8.3) / 8.3;
                double advance = Math.Max(0, Math.Sin(cycle * Math.PI * 2) * 0.7 + 0.3);
                double retreat = Math.Max(0, Math.Sin((cycle - 0.5) * Math.PI * 2));

                // Higher cutoff = brighter pebble texture during retreat
                double cut = 0.03 + 0.08 * retreat + 0.02 * advance;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.6) + np1 * cut * 0.6;

                double env = advance * 0.5 + retreat * 0.8;
                data[i] = (np1 * 0.4 + np2 * 0.6) * env;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "ocean_pebble_shore.wav"), Normalize(data, 0.50));
        }

        // 4. Distant surf — very low, constant rumble
        {
            int dur = 37;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0, np7 = 0, np8 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double swell = 0.6 + 0.2 * Math.Sin(2 * Math.PI * t / 19.3)
                             + 0.15 * Math.Sin(2 * Math.PI * t / 9.7)
                             + 0.05 * Math.Sin(2 * Math.PI * t / 5.1);

                // Extra-heavy filtering for distant feel
                double cut = 0.006;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;
                np7 = np7 * (1 - cut) + np6 * cut;
                np8 = np8 * (1 - cut) + np7 * cut;

                data[i] = np8 * swell;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "ocean_distant_surf.wav"), Normalize(data, 0.45));
        }

        // 5. Coastal breeze — mid-range wind with salt air texture
        {
            int dur = 53;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double gust = 0.4 + 0.3 * Math.Sin(2 * Math.PI * t / 5.3)
                            + 0.2 * Math.Sin(2 * Math.PI * t / 12.7)
                            + 0.1 * Math.Sin(2 * Math.PI * t / 3.1);

                double cut = 0.03 + 0.04 * gust;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                data[i] = (np2 * 0.6 + np3 * 0.4) * gust;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "ocean_coastal_breeze.wav"), Normalize(data, 0.45));
        }
    }

    // ================================================================
    // FOREST — leaves, wind, dripping, birds
    // ================================================================
    static void GenerateForest(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Forest");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Forest ===");

        // 1. Gentle leaf rustle — papery broadband with slow modulation
        {
            int dur = 53;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Irregular rustling — multiple slow LFOs
                double rustle = 0.3 + 0.25 * Math.Sin(2 * Math.PI * t / 3.7)
                              + 0.2 * Math.Sin(2 * Math.PI * t / 7.1 + 0.5)
                              + 0.15 * Math.Sin(2 * Math.PI * t / 1.9 + 2.3)
                              + 0.1 * Math.Sin(2 * Math.PI * t / 0.7 + 1.1);
                rustle = Math.Max(0.05, rustle);

                // Mid-high cutoff for papery texture
                double cut = 0.06 + 0.06 * rustle;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                data[i] = (np1 * 0.3 + np2 * 0.4 + np3 * 0.3) * rustle;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "forest_leaf_rustle.wav"), Normalize(data, 0.45));
        }

        // 2. Canopy drip — after rain, heavy drops on broad leaves
        {
            int dur = 47;
            var data = new double[SR * dur];
            // Pre-generate drip events
            var drips = new List<(double time, double freq, double amp)>();
            double dripTime = 0.5 + rng.NextDouble() * 0.8;
            while (dripTime < dur - 1)
            {
                drips.Add((dripTime, 2000 + rng.NextDouble() * 2500, 0.15 + rng.NextDouble() * 0.25));
                dripTime += 0.3 + rng.NextDouble() * 1.8; // irregular spacing
            }

            double np1 = 0, np2 = 0, np3 = 0, np4 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Subtle forest bed noise floor
                double cut = 0.015 + 0.005 * Math.Sin(2 * Math.PI * t / 8.3);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;

                double sample = np4 * 0.3;

                // Add drip transients
                foreach (var drip in drips)
                {
                    double dt = t - drip.time;
                    if (dt >= 0 && dt < 0.08)
                    {
                        // Drip = descending pitch burst + resonance
                        double dEnv = Math.Exp(-dt * 80) * drip.amp;
                        double dFreq = drip.freq * (1 - dt * 5); // descending pitch
                        sample += Math.Sin(2 * Math.PI * dFreq * dt) * dEnv;
                        // Splash noise
                        double splashNoise = rng.NextDouble() * 2 - 1;
                        sample += splashNoise * Math.Exp(-dt * 120) * drip.amp * 0.3;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "forest_canopy_drip.wav"), Normalize(data, 0.50));
        }

        // 3. Deep forest ambience — low hum with distant bird-like chirps
        {
            int dur = 59;
            var data = new double[SR * dur];
            // Pre-generate sparse chirp events
            var chirps = new List<(double time, double baseFreq)>();
            double chirpTime = 2 + rng.NextDouble() * 3;
            while (chirpTime < dur - 2)
            {
                chirps.Add((chirpTime, 2800 + rng.NextDouble() * 1500));
                chirpTime += 3 + rng.NextDouble() * 6; // very sparse
            }

            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Deep forest drone — very low
                double cut = 0.01 + 0.003 * Math.Sin(2 * Math.PI * t / 11.3);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;

                double sample = np5 * 0.6;

                // Add distant chirps
                foreach (var chirp in chirps)
                {
                    double dt = t - chirp.time;
                    if (dt >= 0 && dt < 0.15)
                    {
                        // Two-note chirp pattern
                        double note1env = Math.Exp(-dt * 40) * Math.Max(0, 1 - dt * 15);
                        double note2env = dt > 0.07 ? Math.Exp(-(dt - 0.07) * 45) : 0;
                        double chirpSample = Math.Sin(2 * Math.PI * chirp.baseFreq * dt) * note1env * 0.08;
                        chirpSample += Math.Sin(2 * Math.PI * chirp.baseFreq * 1.25 * dt) * note2env * 0.06;
                        sample += chirpSample;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "forest_deep_ambience.wav"), Normalize(data, 0.45));
        }

        // 4. Bamboo creek — woody knocks over trickling water
        {
            int dur = 43;
            var data = new double[SR * dur];
            // Pre-generate knock events (like bamboo deer-scarer)
            var knocks = new List<(double time, double freq)>();
            double knockTime = 3 + rng.NextDouble() * 5;
            while (knockTime < dur - 2)
            {
                knocks.Add((knockTime, 300 + rng.NextDouble() * 200));
                knockTime += 5 + rng.NextDouble() * 10;
            }

            double np1 = 0, np2 = 0;
            double waterN1 = 0, waterN2 = 0, waterN3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;
                double noise2 = rng.NextDouble() * 2 - 1;

                // Trickling water — bright but gentle
                double waterCut = 0.08 + 0.04 * Math.Sin(2 * Math.PI * t / 2.3);
                waterN1 = waterN1 * (1 - waterCut) + noise * waterCut;
                waterN2 = waterN2 * (1 - waterCut * 0.6) + waterN1 * waterCut * 0.6;
                waterN3 = waterN3 * (1 - waterCut * 0.4) + waterN2 * waterCut * 0.4;
                double water = waterN3 * (0.15 + 0.05 * Math.Sin(2 * Math.PI * t / 3.7));

                double sample = water;

                // Bamboo knock = woody resonant transient
                foreach (var knock in knocks)
                {
                    double dt = t - knock.time;
                    if (dt >= 0 && dt < 0.25)
                    {
                        double kEnv = Math.Exp(-dt * 25);
                        double kNoise = rng.NextDouble() * 2 - 1;
                        np1 = np1 * 0.5 + kNoise * 0.5;
                        double knockSample = np1 * Math.Exp(-dt * 60) * 0.3; // woody attack
                        knockSample += Math.Sin(2 * Math.PI * knock.freq * dt) * kEnv * 0.15; // resonance
                        knockSample += Math.Sin(2 * Math.PI * knock.freq * 2.3 * dt) * kEnv * 0.05; // overtone
                        sample += knockSample;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "forest_bamboo_creek.wav"), Normalize(data, 0.50));
        }

        // 5. Wind through pines — soft broadband sway
        {
            int dur = 51;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double sway = 0.3 + 0.25 * Math.Sin(2 * Math.PI * t / 4.7)
                            + 0.2 * Math.Sin(2 * Math.PI * t / 9.3 + 0.8)
                            + 0.15 * Math.Sin(2 * Math.PI * t / 2.1 + 1.5)
                            + 0.1 * Math.Sin(2 * Math.PI * t / 15.1);
                sway = Math.Max(0.05, sway);

                double cut = 0.025 + 0.03 * sway;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.8) + np1 * cut * 0.8;
                np3 = np3 * (1 - cut * 0.6) + np2 * cut * 0.6;
                np4 = np4 * (1 - cut * 0.4) + np3 * cut * 0.4;

                data[i] = (np2 * 0.3 + np3 * 0.4 + np4 * 0.3) * sway;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "forest_pine_wind.wav"), Normalize(data, 0.45));
        }
    }

    // ================================================================
    // ZEN — singing bowls, drones, temple silence, meditation
    // ================================================================
    static void GenerateZen(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Zen");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Zen ===");

        // 1. Singing bowl resonance — long decaying harmonics
        {
            int dur = 53;
            var data = new double[SR * dur];
            // Bowl strikes at intervals
            var strikes = new List<(double time, double fundamental)>();
            double sTime = 1.0;
            while (sTime < dur - 6)
            {
                strikes.Add((sTime, 220 + rng.NextDouble() * 80));
                sTime += 8 + rng.NextDouble() * 7; // long gaps between strikes
            }

            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double sample = 0;

                foreach (var strike in strikes)
                {
                    double dt = t - strike.time;
                    if (dt >= 0 && dt < 12.0) // very long decay
                    {
                        double env = Math.Exp(-dt * 0.3); // slow decay
                        // Singing bowl = fundamental + partials at ~2.71x, 4.8x, 7.6x
                        sample += Math.Sin(2 * Math.PI * strike.fundamental * dt) * env * 0.4;
                        sample += Math.Sin(2 * Math.PI * strike.fundamental * 2.71 * dt) * env * 0.25;
                        sample += Math.Sin(2 * Math.PI * strike.fundamental * 4.8 * dt) * env * 0.12;
                        sample += Math.Sin(2 * Math.PI * strike.fundamental * 7.6 * dt) * env * 0.05;
                        // Subtle beating between close partials
                        sample += Math.Sin(2 * Math.PI * (strike.fundamental * 2.71 + 1.5) * dt) * env * 0.08;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 4.0);
            WriteWav(Path.Combine(dir, "zen_singing_bowl.wav"), Normalize(data, 0.45));
        }

        // 2. Ethereal pad — warm synth drone, no melody
        {
            int dur = 59;
            var data = new double[SR * dur];
            double baseFreq = 110; // A2
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;

                // Slow modulation of volume and timbre
                double modA = 0.5 + 0.3 * Math.Sin(2 * Math.PI * t / 17.3)
                            + 0.2 * Math.Sin(2 * Math.PI * t / 23.1);

                // Stacked fifths drone
                double drone = Math.Sin(2 * Math.PI * baseFreq * t) * 0.3;
                drone += Math.Sin(2 * Math.PI * baseFreq * 1.5 * t) * 0.2;   // perfect fifth
                drone += Math.Sin(2 * Math.PI * baseFreq * 2.0 * t) * 0.15;  // octave
                drone += Math.Sin(2 * Math.PI * baseFreq * 3.0 * t) * 0.08;  // octave + fifth
                // Subtle detuning for warmth
                drone += Math.Sin(2 * Math.PI * (baseFreq + 0.5) * t) * 0.1;
                drone += Math.Sin(2 * Math.PI * (baseFreq * 1.5 + 0.3) * t) * 0.07;

                data[i] = drone * modA;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(dir, "zen_ethereal_pad.wav"), Normalize(data, 0.40));
        }

        // 3. Temple room tone — near-silence with subtle air
        {
            int dur = 41;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0, np7 = 0, np8 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Extremely gentle — the sound of "silence" in a large stone room
                double cut = 0.004;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;
                np7 = np7 * (1 - cut) + np6 * cut;
                np8 = np8 * (1 - cut) + np7 * cut;

                // 60 Hz electrical hum (very faint, adds "room" character)
                double hum = Math.Sin(2 * Math.PI * 60 * t) * 0.008;

                double breathe = 0.7 + 0.3 * Math.Sin(2 * Math.PI * t / 19.7);

                data[i] = np8 * breathe + hum;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "zen_temple_room_tone.wav"), Normalize(data, 0.30));
        }

        // 4. Wind chimes — sparse metallic tings in breeze
        {
            int dur = 47;
            var data = new double[SR * dur];
            // Pre-generate chime events
            var chimes = new List<(double time, double freq, double amp)>();
            double chimeTime = 1.5;
            while (chimeTime < dur - 2)
            {
                chimes.Add((chimeTime, 1200 + rng.NextDouble() * 2000, 0.1 + rng.NextDouble() * 0.15));
                // Sometimes cluster 2-3 chimes together
                if (rng.NextDouble() > 0.5)
                    chimes.Add((chimeTime + 0.1 + rng.NextDouble() * 0.3, 1200 + rng.NextDouble() * 2000, 0.08 + rng.NextDouble() * 0.1));
                chimeTime += 2 + rng.NextDouble() * 5;
            }

            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Gentle breeze floor
                double cut = 0.012 + 0.008 * Math.Sin(2 * Math.PI * t / 6.3);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;

                double sample = np5 * 0.3;

                // Metallic chime = high freq with long decay + beating partials
                foreach (var chime in chimes)
                {
                    double dt = t - chime.time;
                    if (dt >= 0 && dt < 3.0)
                    {
                        double env = Math.Exp(-dt * 1.5) * chime.amp;
                        sample += Math.Sin(2 * Math.PI * chime.freq * dt) * env;
                        sample += Math.Sin(2 * Math.PI * chime.freq * 2.76 * dt) * env * 0.3;
                        sample += Math.Sin(2 * Math.PI * chime.freq * 5.4 * dt) * env * 0.1;
                        // Beating from close partial
                        sample += Math.Sin(2 * Math.PI * (chime.freq + 2) * dt) * env * 0.15;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "zen_wind_chimes.wav"), Normalize(data, 0.45));
        }

        // 5. Deep om drone — binaural-ish low hum
        {
            int dur = 61;
            var data = new double[SR * dur];
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;

                // Very slow breathing modulation
                double breathe = 0.5 + 0.3 * Math.Sin(2 * Math.PI * t / 13.7)
                                + 0.2 * Math.Sin(2 * Math.PI * t / 21.3);

                // Low fundamental with slight detuning for richness
                double om = Math.Sin(2 * Math.PI * 65 * t) * 0.35;       // C2
                om += Math.Sin(2 * Math.PI * 65.5 * t) * 0.15;           // detune → beating
                om += Math.Sin(2 * Math.PI * 130 * t) * 0.12;            // octave
                om += Math.Sin(2 * Math.PI * 195 * t) * 0.06;            // 5th
                om += Math.Sin(2 * Math.PI * 97.5 * t) * 0.08;           // 3/2 sub-harmonic feel
                om += Math.Sin(2 * Math.PI * 130.7 * t) * 0.05;          // detune octave → beating

                data[i] = om * breathe;
            }
            ApplyLoopFade(data, 5.0);
            WriteWav(Path.Combine(dir, "zen_om_drone.wav"), Normalize(data, 0.40));
        }

        // 6. Sand garden raking — dry scratching rhythm
        {
            int dur = 43;
            var data = new double[SR * dur];
            double np1 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Slow, rhythmic raking strokes (~4 sec each)
                double cycle = (t % 4.3) / 4.3;
                // Stroke shape: silence → build → peak → taper
                double stroke = 0;
                if (cycle > 0.1 && cycle < 0.7)
                {
                    double phase = (cycle - 0.1) / 0.6;
                    stroke = Math.Sin(phase * Math.PI); // bell curve through stroke
                }

                // Gritty texture — mid/high noise
                double cut = 0.1 + 0.15 * stroke;
                np1 = np1 * (1 - cut) + noise * cut;

                data[i] = np1 * stroke * 0.4;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "zen_sand_raking.wav"), Normalize(data, 0.40));
        }
    }

    // ================================================================
    // MOUNTAIN — wind, altitude, isolation
    // ================================================================
    static void GenerateMountain(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Mountain");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Mountain ===");

        // 1. Summit gale — howling wind with whistling overtones
        {
            int dur = 53;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Strong gusts with dramatic swells
                double gust = 0.3 + 0.35 * Math.Sin(2 * Math.PI * t / 3.7)
                            + 0.2 * Math.Sin(2 * Math.PI * t / 8.3 + 1.5)
                            + 0.15 * Math.Sin(2 * Math.PI * t / 1.3 + 0.7);
                gust = Math.Max(0.05, gust);

                double cut = 0.04 + 0.06 * gust;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                // Wind howl = resonant peaks from rock formations
                double howl = Math.Sin(2 * Math.PI * (400 + 200 * gust) * t) * gust * 0.04;
                howl += Math.Sin(2 * Math.PI * (700 + 150 * gust) * t) * gust * 0.02;

                data[i] = (np1 * 0.4 + np2 * 0.4 + np3 * 0.2) * gust + howl;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "mountain_summit_gale.wav"), Normalize(data, 0.50));
        }

        // 2. Alpine meadow — gentle wind + distant rumble
        {
            int dur = 47;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double breeze = 0.4 + 0.2 * Math.Sin(2 * Math.PI * t / 6.7)
                              + 0.15 * Math.Sin(2 * Math.PI * t / 13.1)
                              + 0.1 * Math.Sin(2 * Math.PI * t / 3.3);

                // Two layers: gentle wind + deep sub-bass rumble
                double windCut = 0.03 + 0.03 * breeze;
                np1 = np1 * (1 - windCut) + noise * windCut;
                np2 = np2 * (1 - windCut * 0.7) + np1 * windCut * 0.7;

                double deepCut = 0.005;
                np3 = np3 * (1 - deepCut) + noise * deepCut;
                np4 = np4 * (1 - deepCut) + np3 * deepCut;
                np5 = np5 * (1 - deepCut) + np4 * deepCut;

                data[i] = (np1 * 0.3 + np2 * 0.4) * breeze + np5 * 0.2;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "mountain_alpine_meadow.wav"), Normalize(data, 0.45));
        }

        // 3. Mountain stream — fast-flowing water over rocks
        {
            int dur = 41;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Water texture — brighter than ocean, more chaotic
                double flow = 0.5 + 0.2 * Math.Sin(2 * Math.PI * t / 2.7)
                            + 0.15 * Math.Sin(2 * Math.PI * t / 0.9)
                            + 0.1 * Math.Sin(2 * Math.PI * t / 5.3);

                double cut = 0.07 + 0.06 * flow;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.6) + np1 * cut * 0.6;
                np3 = np3 * (1 - cut * 0.4) + np2 * cut * 0.4;

                data[i] = (np1 * 0.25 + np2 * 0.45 + np3 * 0.3) * flow;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "mountain_stream.wav"), Normalize(data, 0.50));
        }

        // 4. High altitude silence — thin air, near-silence with rare gusts
        {
            int dur = 37;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Mostly silent with slow, rare swells
                double swell = 0.08 + 0.15 * Math.Max(0, Math.Sin(2 * Math.PI * t / 11.3))
                             + 0.1 * Math.Max(0, Math.Sin(2 * Math.PI * t / 17.7 + 2.0));

                double cut = 0.008 + 0.01 * swell;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;

                data[i] = np6 * swell;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "mountain_high_altitude.wav"), Normalize(data, 0.35));
        }

        // 5. Distant thunder — sub-bass rumbles
        {
            int dur = 59;
            var data = new double[SR * dur];
            // Pre-generate thunder events
            var thunders = new List<(double time, double freq, double dur)>();
            double tTime = 5 + rng.NextDouble() * 5;
            while (tTime < dur - 5)
            {
                thunders.Add((tTime, 30 + rng.NextDouble() * 30, 2 + rng.NextDouble() * 3));
                tTime += 8 + rng.NextDouble() * 12;
            }

            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Light wind floor
                double cut = 0.01;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;

                double sample = np6 * 0.15;

                foreach (var thunder in thunders)
                {
                    double dt = t - thunder.time;
                    if (dt >= 0 && dt < thunder.dur)
                    {
                        double tNorm = dt / thunder.dur;
                        // Rumble envelope: slow attack, long tail
                        double tEnv = Math.Sin(tNorm * Math.PI) * Math.Exp(-tNorm * 2);
                        // Deep rumble = filtered noise + sub tone
                        double rumbleNoise = rng.NextDouble() * 2 - 1;
                        double rn1 = rumbleNoise * 0.01;
                        sample += rn1 * tEnv * 0.4;
                        sample += Math.Sin(2 * Math.PI * thunder.freq * dt) * tEnv * 0.25;
                        sample += Math.Sin(2 * Math.PI * thunder.freq * 1.5 * dt) * tEnv * 0.1;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "mountain_distant_thunder.wav"), Normalize(data, 0.45));
        }
    }

    // ================================================================
    // RAIN — light, steady, heavy, tin roof
    // ================================================================
    static void GenerateRain(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Rain");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Rain ===");

        // 1. Light rain — gentle patter, sparse drops
        {
            int dur = 47;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            // Generate raindrop impacts
            var drops = new List<(double time, double freq, double amp)>();
            double dropTime = 0.05;
            while (dropTime < dur - 0.5)
            {
                drops.Add((dropTime, 3000 + rng.NextDouble() * 3000, 0.03 + rng.NextDouble() * 0.06));
                dropTime += 0.02 + rng.NextDouble() * 0.15;
            }

            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Very subtle noise floor
                np1 = np1 * 0.98 + noise * 0.02;
                np2 = np2 * 0.98 + np1 * 0.02;
                np3 = np3 * 0.98 + np2 * 0.02;

                double sample = np3 * 0.05;

                // Individual drop impacts
                foreach (var drop in drops)
                {
                    double dt = t - drop.time;
                    if (dt >= 0 && dt < 0.015)
                    {
                        double dEnv = Math.Exp(-dt * 300);
                        sample += Math.Sin(2 * Math.PI * drop.freq * dt) * dEnv * drop.amp;
                        // Tiny splash
                        sample += (rng.NextDouble() * 2 - 1) * dEnv * drop.amp * 0.4;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "rain_light_patter.wav"), Normalize(data, 0.45));
        }

        // 2. Steady rain — constant broadband wash
        {
            int dur = 53;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Rain intensity modulation
                double intensity = 0.6 + 0.15 * Math.Sin(2 * Math.PI * t / 7.3)
                                 + 0.1 * Math.Sin(2 * Math.PI * t / 13.7)
                                 + 0.1 * Math.Sin(2 * Math.PI * t / 3.1)
                                 + 0.05 * Math.Sin(2 * Math.PI * t / 1.3);

                double cut = 0.05 + 0.04 * intensity;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                data[i] = (np1 * 0.3 + np2 * 0.4 + np3 * 0.3) * intensity;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "rain_steady.wav"), Normalize(data, 0.50));
        }

        // 3. Heavy downpour — dense, loud, immersive
        {
            int dur = 41;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double intensity = 0.7 + 0.15 * Math.Sin(2 * Math.PI * t / 5.7)
                                 + 0.1 * Math.Sin(2 * Math.PI * t / 11.3)
                                 + 0.05 * Math.Sin(2 * Math.PI * t / 2.1);

                // Higher cutoff = brighter, denser rain
                double cut = 0.08 + 0.06 * intensity;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.6) + np1 * cut * 0.6;

                data[i] = (np1 * 0.5 + np2 * 0.5) * intensity;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "rain_heavy_downpour.wav"), Normalize(data, 0.55));
        }

        // 4. Rain on tin roof — metallic resonance + patter
        {
            int dur = 43;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0;
            // Generate metallic impact events
            var impacts = new List<(double time, double freq, double amp)>();
            double impTime = 0.03;
            while (impTime < dur - 0.5)
            {
                impacts.Add((impTime, 800 + rng.NextDouble() * 1500, 0.04 + rng.NextDouble() * 0.08));
                impTime += 0.03 + rng.NextDouble() * 0.12;
            }

            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Rain wash noise floor
                double cut = 0.04 + 0.02 * Math.Sin(2 * Math.PI * t / 5.1);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                double sample = (np1 * 0.3 + np2 * 0.4) * 0.5;

                // Metallic impacts — resonant pings
                foreach (var imp in impacts)
                {
                    double dt = t - imp.time;
                    if (dt >= 0 && dt < 0.04)
                    {
                        double env = Math.Exp(-dt * 120);
                        sample += Math.Sin(2 * Math.PI * imp.freq * dt) * env * imp.amp;
                        sample += Math.Sin(2 * Math.PI * imp.freq * 2.3 * dt) * env * imp.amp * 0.3;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "rain_tin_roof.wav"), Normalize(data, 0.50));
        }

        // 5. Thunderstorm — rain + distant rolling thunder
        {
            int dur = 59;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0;
            // Thunder rumbles
            var thunders = new List<(double time, double dur)>();
            double tTime = 4 + rng.NextDouble() * 5;
            while (tTime < dur - 4)
            {
                thunders.Add((tTime, 2 + rng.NextDouble() * 3));
                tTime += 7 + rng.NextDouble() * 10;
            }

            double tn1 = 0, tn2 = 0, tn3 = 0, tn4 = 0, tn5 = 0, tn6 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Steady heavy rain
                double cut = 0.06 + 0.03 * Math.Sin(2 * Math.PI * t / 4.7);
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                double sample = (np1 * 0.2 + np2 * 0.4 + np3 * 0.3) * 0.6;

                // Rolling thunder
                double thunderNoise = rng.NextDouble() * 2 - 1;
                double tCut = 0.005;
                tn1 = tn1 * (1 - tCut) + thunderNoise * tCut;
                tn2 = tn2 * (1 - tCut) + tn1 * tCut;
                tn3 = tn3 * (1 - tCut) + tn2 * tCut;
                tn4 = tn4 * (1 - tCut) + tn3 * tCut;
                tn5 = tn5 * (1 - tCut) + tn4 * tCut;
                tn6 = tn6 * (1 - tCut) + tn5 * tCut;

                foreach (var thunder in thunders)
                {
                    double dt = t - thunder.time;
                    if (dt >= 0 && dt < thunder.dur)
                    {
                        double tNorm = dt / thunder.dur;
                        double tEnv = Math.Sin(tNorm * Math.PI) * Math.Exp(-tNorm * 1.5);
                        sample += tn6 * tEnv * 0.6;
                        sample += Math.Sin(2 * Math.PI * 40 * dt) * tEnv * 0.15;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "rain_thunderstorm.wav"), Normalize(data, 0.55));
        }
    }

    // ================================================================
    // NIGHT — crickets, frogs, nocturnal ambience
    // ================================================================
    static void GenerateNight(string ambDir)
    {
        var dir = Path.Combine(ambDir, "Night");
        Directory.CreateDirectory(dir);
        Console.WriteLine("\n=== Night ===");

        // 1. Cricket chorus — rhythmic chirping layered at different rates
        {
            int dur = 47;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Night noise floor — very subtle
                double cut = 0.008;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;

                double sample = np4 * 0.15;

                // 3 cricket "voices" at different chirp rates
                // Cricket chirp = burst of high-freq tone
                double[] rates = { 2.3, 3.1, 1.7 };
                double[] freqs = { 4200, 4800, 3800 };
                double[] amps = { 0.08, 0.06, 0.05 };
                for (int c = 0; c < 3; c++)
                {
                    double chirpPhase = (t % rates[c]) / rates[c];
                    // Each "chirp" is 2-3 short pulses
                    double chirp = 0;
                    for (int p = 0; p < 3; p++)
                    {
                        double pulseCenter = 0.05 + p * 0.06;
                        double pDist = Math.Abs(chirpPhase - pulseCenter);
                        if (pDist < 0.025)
                        {
                            double pEnv = 1 - pDist / 0.025;
                            chirp += Math.Sin(2 * Math.PI * freqs[c] * t) * pEnv * amps[c];
                        }
                    }
                    sample += chirp;
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "night_cricket_chorus.wav"), Normalize(data, 0.45));
        }

        // 2. Frog pond — deep croaking over still water
        {
            int dur = 53;
            var data = new double[SR * dur];
            // Pre-generate frog croak events
            var croaks = new List<(double time, double freq, double dur)>();
            double croakTime = 1.0;
            while (croakTime < dur - 2)
            {
                croaks.Add((croakTime, 120 + rng.NextDouble() * 80, 0.3 + rng.NextDouble() * 0.4));
                // Sometimes rapid-fire croaks
                if (rng.NextDouble() > 0.6)
                {
                    for (int burst = 0; burst < 2 + rng.Next(3); burst++)
                    {
                        croakTime += 0.4 + rng.NextDouble() * 0.3;
                        if (croakTime < dur - 2)
                            croaks.Add((croakTime, 120 + rng.NextDouble() * 80, 0.2 + rng.NextDouble() * 0.3));
                    }
                }
                croakTime += 2 + rng.NextDouble() * 5;
            }

            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Still water / night air floor
                double cut = 0.006;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;
                double sample = np6 * 0.15;

                foreach (var croak in croaks)
                {
                    double dt = t - croak.time;
                    if (dt >= 0 && dt < croak.dur)
                    {
                        double cNorm = dt / croak.dur;
                        double cEnv = Math.Sin(cNorm * Math.PI); // bell-shaped
                        // Frog = low tone with rapid amplitude modulation (throat vibration)
                        double modRate = 30 + rng.NextDouble() * 10;
                        double mod = 0.5 + 0.5 * Math.Sin(2 * Math.PI * modRate * dt);
                        sample += Math.Sin(2 * Math.PI * croak.freq * dt) * cEnv * mod * 0.12;
                        sample += Math.Sin(2 * Math.PI * croak.freq * 2 * dt) * cEnv * mod * 0.04;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "night_frog_pond.wav"), Normalize(data, 0.45));
        }

        // 3. Summer night — combined crickets, gentle breeze, distant owl
        {
            int dur = 59;
            var data = new double[SR * dur];
            // Owl hoots
            var hoots = new List<(double time, double freq)>();
            double hootTime = 5 + rng.NextDouble() * 8;
            while (hootTime < dur - 3)
            {
                hoots.Add((hootTime, 350 + rng.NextDouble() * 50));
                if (rng.NextDouble() > 0.4) // often 2 hoots
                    hoots.Add((hootTime + 0.8, 320 + rng.NextDouble() * 50));
                hootTime += 10 + rng.NextDouble() * 15;
            }

            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Night breeze
                double breeze = 0.2 + 0.1 * Math.Sin(2 * Math.PI * t / 7.3)
                              + 0.08 * Math.Sin(2 * Math.PI * t / 3.1);
                double cut = 0.015 + 0.01 * breeze;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;

                double sample = np3 * breeze;

                // Subtle cricket layer
                double chirpPhase = (t % 2.7) / 2.7;
                if (chirpPhase < 0.15)
                {
                    double cEnv = Math.Sin(chirpPhase / 0.15 * Math.PI);
                    sample += Math.Sin(2 * Math.PI * 4500 * t) * cEnv * 0.03;
                }

                // Owl hoots — breathy, low tone
                foreach (var hoot in hoots)
                {
                    double dt = t - hoot.time;
                    if (dt >= 0 && dt < 0.5)
                    {
                        double hEnv = Math.Sin(dt / 0.5 * Math.PI) * 0.08;
                        sample += Math.Sin(2 * Math.PI * hoot.freq * dt) * hEnv;
                        // Breathy noise component
                        sample += (rng.NextDouble() * 2 - 1) * hEnv * 0.15;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "night_summer_ambience.wav"), Normalize(data, 0.45));
        }

        // 4. Campfire — crackling, popping, warm
        {
            int dur = 43;
            var data = new double[SR * dur];
            // Generate crackle/pop events
            var pops = new List<(double time, double freq, double amp)>();
            double popTime = 0.2;
            while (popTime < dur - 0.5)
            {
                pops.Add((popTime, 1500 + rng.NextDouble() * 3000, 0.05 + rng.NextDouble() * 0.15));
                popTime += 0.05 + rng.NextDouble() * 0.4;
            }

            double np1 = 0, np2 = 0, np3 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                // Fire roar base — warm low-mid noise
                double roar = 0.4 + 0.15 * Math.Sin(2 * Math.PI * t / 3.7)
                            + 0.1 * Math.Sin(2 * Math.PI * t / 7.1);
                double cut = 0.03 + 0.02 * roar;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut * 0.7) + np1 * cut * 0.7;
                np3 = np3 * (1 - cut * 0.5) + np2 * cut * 0.5;

                double sample = (np2 * 0.4 + np3 * 0.4) * roar * 0.5;

                // Crackle pops
                foreach (var pop in pops)
                {
                    double dt = t - pop.time;
                    if (dt >= 0 && dt < 0.02)
                    {
                        double pEnv = Math.Exp(-dt * 200);
                        sample += (rng.NextDouble() * 2 - 1) * pEnv * pop.amp;
                        sample += Math.Sin(2 * Math.PI * pop.freq * dt) * pEnv * pop.amp * 0.3;
                    }
                }

                data[i] = sample;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "night_campfire.wav"), Normalize(data, 0.50));
        }

        // 5. Stargazing — near-total silence with occasional breeze
        {
            int dur = 37;
            var data = new double[SR * dur];
            double np1 = 0, np2 = 0, np3 = 0, np4 = 0, np5 = 0, np6 = 0, np7 = 0, np8 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / SR;
                double noise = rng.NextDouble() * 2 - 1;

                double swell = 0.05 + 0.08 * Math.Max(0, Math.Sin(2 * Math.PI * t / 13.7))
                             + 0.05 * Math.Max(0, Math.Sin(2 * Math.PI * t / 23.1 + 1.5));

                double cut = 0.005 + 0.005 * swell;
                np1 = np1 * (1 - cut) + noise * cut;
                np2 = np2 * (1 - cut) + np1 * cut;
                np3 = np3 * (1 - cut) + np2 * cut;
                np4 = np4 * (1 - cut) + np3 * cut;
                np5 = np5 * (1 - cut) + np4 * cut;
                np6 = np6 * (1 - cut) + np5 * cut;
                np7 = np7 * (1 - cut) + np6 * cut;
                np8 = np8 * (1 - cut) + np7 * cut;

                data[i] = np8 * swell;
            }
            ApplyLoopFade(data, 3.0);
            WriteWav(Path.Combine(dir, "night_stargazing.wav"), Normalize(data, 0.30));
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    /// <summary>
    /// Apply fade-in at the start and fade-out at the end for seamless looping.
    /// </summary>
    static void ApplyLoopFade(double[] data, double fadeSeconds)
    {
        int fadeSamples = (int)(SR * fadeSeconds);
        fadeSamples = Math.Min(fadeSamples, data.Length / 4);

        // Fade in
        for (int i = 0; i < fadeSamples; i++)
        {
            double fade = (double)i / fadeSamples;
            fade = fade * fade; // ease-in (quadratic)
            data[i] *= fade;
        }

        // Fade out
        for (int i = 0; i < fadeSamples; i++)
        {
            int idx = data.Length - 1 - i;
            double fade = (double)i / fadeSamples;
            fade = fade * fade; // ease-out (quadratic)
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
        bw.Write((short)1);       // PCM
        bw.Write((short)1);       // mono
        bw.Write(SR);             // sample rate
        bw.Write(SR * 2);         // byte rate
        bw.Write((short)2);       // block align
        bw.Write((short)16);      // bits per sample
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (var s in samples)
            bw.Write(s);

        short peak = 0;
        foreach (var s in samples)
            if (Math.Abs(s) > peak) peak = Math.Abs(s);
        double peakDb = peak > 0 ? 20 * Math.Log10((double)peak / 32767) : -99;
        double durSec = (double)samples.Length / SR;

        string scape = Path.GetFileName(Path.GetDirectoryName(path))!;
        string file = Path.GetFileNameWithoutExtension(path);
        Console.WriteLine($"  {scape}/{file}: {durSec:F1}s, peak={peak} ({peakDb:F1} dB)");
    }
}
