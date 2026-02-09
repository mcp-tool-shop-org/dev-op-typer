namespace DevOpTyper.Models;

/// <summary>
/// An alternative approach to the same coding problem.
/// Demonstrations show "how else" — they present different valid ways
/// to write the same functionality, without ranking or recommending.
///
/// Labels describe the approach ("Functional style", "Imperative approach"),
/// never the author. Demonstrations carry no authorship or origin metadata.
/// During import, PortableBundleService deserializes into this model, which
/// strips any non-schema fields. The approach stands on its own.
/// </summary>
public sealed class Demonstration
{
    /// <summary>
    /// Short label describing this approach.
    /// Examples: "Functional style", "With early return", "Using pattern matching".
    /// Never an author name — approaches are about code, not people.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// The alternative code implementing the same functionality.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Optional brief description of why this approach differs.
    /// </summary>
    public string? Description { get; set; }
}
