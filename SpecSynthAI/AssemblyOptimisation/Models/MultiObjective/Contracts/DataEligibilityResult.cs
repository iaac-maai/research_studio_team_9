namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

/// <summary>
/// Explains whether a record may be used for a requested purpose.
/// </summary>
public sealed class DataEligibilityResult
{
    public bool IsEligible { get; init; }

    public string Reason { get; init; } = "";

    public static DataEligibilityResult Allow(string reason) =>
        new() { IsEligible = true, Reason = reason };

    public static DataEligibilityResult Deny(string reason) =>
        new() { IsEligible = false, Reason = reason };
}
