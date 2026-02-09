namespace DevOpTyper.Models;

/// <summary>
/// Controls whether learning signals influence practice behavior.
/// All flags default to false — v0.9 behavior is the baseline.
/// When GuidedMode is off, all flags remain false regardless of state.
/// </summary>
public sealed class SignalPolicy
{
    /// <summary>
    /// Master switch: when false, all signal-based behavior is disabled.
    /// Equivalent to v0.9 mode. User must explicitly opt in.
    /// </summary>
    public bool GuidedMode { get; set; } = false;

    /// <summary>
    /// When true (and GuidedMode is on), snippet selection within the
    /// chosen difficulty band prefers snippets matching weakness categories.
    /// Never changes the difficulty band itself.
    /// </summary>
    public bool SignalsAffectSelection { get; set; } = false;

    /// <summary>
    /// Reserved for future use. When true (and GuidedMode is on),
    /// signals could influence target difficulty. Currently always false.
    /// </summary>
    public bool SignalsAffectDifficulty { get; set; } = false;

    /// <summary>
    /// Reserved for future use. When true (and GuidedMode is on),
    /// signals could influence XP calculation. Currently always false.
    /// </summary>
    public bool SignalsAffectXP { get; set; } = false;

    /// <summary>
    /// Returns the effective selection bias state: true only when
    /// GuidedMode AND SignalsAffectSelection are both enabled.
    /// </summary>
    public bool EffectiveSelectionBias => GuidedMode && SignalsAffectSelection;

    /// <summary>
    /// Returns the effective difficulty influence state.
    /// Currently always false (reserved).
    /// </summary>
    public bool EffectiveDifficultyInfluence => GuidedMode && SignalsAffectDifficulty;

    /// <summary>
    /// Returns the effective XP influence state.
    /// Currently always false (reserved).
    /// </summary>
    public bool EffectiveXPInfluence => GuidedMode && SignalsAffectXP;

    /// <summary>
    /// Enables Guided Mode with default settings:
    /// selection bias on, difficulty and XP influence off.
    /// </summary>
    public void EnableGuidedMode()
    {
        GuidedMode = true;
        SignalsAffectSelection = true;
        SignalsAffectDifficulty = false;
        SignalsAffectXP = false;
    }

    /// <summary>
    /// Disables Guided Mode. All influence flags become ineffective
    /// (the Effective* properties all return false).
    /// </summary>
    public void DisableGuidedMode()
    {
        GuidedMode = false;
    }

    /// <summary>
    /// Creates a deep copy of this policy.
    /// </summary>
    public SignalPolicy Clone()
    {
        return new SignalPolicy
        {
            GuidedMode = GuidedMode,
            SignalsAffectSelection = SignalsAffectSelection,
            SignalsAffectDifficulty = SignalsAffectDifficulty,
            SignalsAffectXP = SignalsAffectXP
        };
    }
}
