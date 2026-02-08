namespace DevOpTyper.Models;

public sealed class Profile
{
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;

    // A simple per-language rating; expand later to Elo-like.
    public Dictionary<string, int> RatingByLanguage { get; set; } = new()
    {
        ["python"] = 1200,
        ["java"] = 1200
    };

    public void AddXp(int amount)
    {
        Xp += Math.Max(0, amount);

        // Very simple leveling curve (tune later)
        while (Xp >= XpNeededForNext(Level))
        {
            Xp -= XpNeededForNext(Level);
            Level++;
        }
    }

    public static int XpNeededForNext(int level) => 200 + (level * 40);
}
