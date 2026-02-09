namespace DevOpTyper.Models;

/// <summary>
/// A depth tier of content for a snippet. Labels describe the content's
/// depth ("Essentials", "Deeper", "Advanced"), never the user's level.
///
/// All layers are accessible to all users at all times. No layer is gated
/// by user level, rating, or session count. The system never recommends
/// which layers to read.
/// </summary>
public sealed class SkillLayer
{
    /// <summary>
    /// Describes what depth this content covers.
    /// Labels like "Essentials", "Deeper", "Advanced", "Performance".
    /// Never labels like "Beginner", "Expert", "Your Level".
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Content items at this depth tier.
    /// Each item is a short observation or explanation.
    /// </summary>
    public string[] Content { get; set; } = Array.Empty<string>();
}
