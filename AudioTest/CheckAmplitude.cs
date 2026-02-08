using System;
using System.IO;

class CheckAmplitude
{
    static void Main()
    {
        CheckFile(Path.GetFullPath("../DevOpTyper/Assets/Sounds/Sfx/key_01.wav"));
        CheckFile(Path.GetFullPath("../DevOpTyper/Assets/Sounds/Sfx/key_05.wav"));
        CheckFile(Path.GetFullPath("../DevOpTyper/Assets/Sounds/Sfx/ui_click.wav"));
        CheckFile(Path.GetFullPath("../DevOpTyper/Assets/Sounds/Ambient/ambient_01.wav"));
        CheckFile(Path.GetFullPath("../DevOpTyper/Assets/Sounds/Ambient/ambient_03.wav"));
    }

    static void CheckFile(string path)
    {
        if (!File.Exists(path)) { Console.WriteLine($"NOT FOUND: {path}"); return; }
        var bytes = File.ReadAllBytes(path);
        // Parse WAV: samples start at byte 44 for standard WAV
        int sampleCount = (bytes.Length - 44) / 2;
        short peak = 0;
        double sumSq = 0;
        for (int i = 44; i < bytes.Length - 1; i += 2)
        {
            short s = BitConverter.ToInt16(bytes, i);
            short abs = Math.Abs(s);
            if (abs > peak) peak = abs;
            sumSq += (double)s * s;
        }
        double rms = Math.Sqrt(sumSq / sampleCount);
        double peakDb = 20 * Math.Log10((double)peak / 32767);
        double rmsDb = 20 * Math.Log10(rms / 32767);

        Console.WriteLine($"{Path.GetFileName(path)}: samples={sampleCount}, peak={peak} ({peakDb:F1} dB), rms={rms:F0} ({rmsDb:F1} dB)");
    }
}
