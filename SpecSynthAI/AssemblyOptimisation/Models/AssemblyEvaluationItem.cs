using SpecSynthAI.AssemblyOptimisation.Models;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Models;

/// <summary>
/// Connects one generated candidate with its existing ThermalEngine result.
/// </summary>
public sealed class AssemblyEvaluationItem
{
    public string CandidateId { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public string CandidateStatus { get; set; } = "";
    public string CompatibilityNote { get; set; } = "";
    public WallComparisonResult ThermalResult { get; set; } = new();
}
