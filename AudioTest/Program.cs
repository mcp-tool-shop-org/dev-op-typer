using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    static extern int mciSendString(string command, StringBuilder? ret, int retSize, IntPtr cb);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    const uint SND_FILENAME = 0x00020000;
    const uint SND_SYNC = 0x0000;
    const uint SND_ASYNC = 0x0001;
    const uint SND_NODEFAULT = 0x0002;

    static void Main()
    {
        string sfxDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "DevOpTyper", "Assets", "Sounds", "Sfx"));
        string ambDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "DevOpTyper", "Assets", "Sounds", "Ambient"));

        string keyFile = Path.Combine(sfxDir, "key_01.wav");
        string ambFile = Path.Combine(ambDir, "ambient_01.wav");

        Console.WriteLine($"Key file: {keyFile}");
        Console.WriteLine($"  Exists: {File.Exists(keyFile)}");
        if (File.Exists(keyFile))
        {
            var fi = new FileInfo(keyFile);
            Console.WriteLine($"  Size: {fi.Length} bytes");
            var bytes = File.ReadAllBytes(keyFile);
            bool allZero = true;
            for (int i = 44; i < Math.Min(bytes.Length, 200); i++)
                if (bytes[i] != 0) { allZero = false; break; }
            Console.WriteLine($"  Audio data is all zeros: {allZero}");
        }

        Console.WriteLine($"\nAmb file: {ambFile}");
        Console.WriteLine($"  Exists: {File.Exists(ambFile)}");
        if (File.Exists(ambFile))
        {
            var fi = new FileInfo(ambFile);
            Console.WriteLine($"  Size: {fi.Length} bytes");
        }

        Console.WriteLine("\n=== TEST 1: PlaySound SYNC (key click - should hear a tick) ===");
        bool r1 = PlaySound(keyFile, IntPtr.Zero, SND_FILENAME | SND_SYNC | SND_NODEFAULT);
        Console.WriteLine($"  Result: {r1}");

        Console.WriteLine("\n=== TEST 2: PlaySound ASYNC (key click) ===");
        bool r2 = PlaySound(keyFile, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT);
        Console.WriteLine($"  Result: {r2}");
        Thread.Sleep(500);

        Console.WriteLine("\n=== TEST 3: mciSendString (ambient - 3 second play) ===");
        int m1 = mciSendString($"open \"{ambFile}\" type waveaudio alias testAmb", null, 0, IntPtr.Zero);
        Console.WriteLine($"  mci open: {m1} (0=success)");
        if (m1 == 0)
        {
            mciSendString("setaudio testAmb volume to 900", null, 0, IntPtr.Zero);
            int m2 = mciSendString("play testAmb", null, 0, IntPtr.Zero);
            Console.WriteLine($"  mci play: {m2} (0=success)");
            Console.WriteLine("  Listening for 3 seconds...");
            Thread.Sleep(3000);
            mciSendString("stop testAmb", null, 0, IntPtr.Zero);
            mciSendString("close testAmb", null, 0, IntPtr.Zero);
        }

        Console.WriteLine("\n=== TEST 4: System.Media.SoundPlayer ===");
        try
        {
            var player = new System.Media.SoundPlayer(keyFile);
            player.PlaySync();
            Console.WriteLine("  SoundPlayer: completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  SoundPlayer FAILED: {ex.Message}");
        }

        Console.WriteLine("\n=== DONE ===");
        Console.WriteLine("Did you hear ANYTHING during any of the 4 tests?");
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
