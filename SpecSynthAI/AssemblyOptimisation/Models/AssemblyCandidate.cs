using System.Collections.Generic;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Models;

/// <summary>
/// A generated research candidate. Candidate status is deliberately separate
/// from ThermalEngine calculation validity and does not imply system approval.
/// </summary>
public sealed class AssemblyCandidate
{
    public string CandidateId { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public string CandidateStatus { get; set; } = "RESEARCH_CANDIDATE";
    public string CompatibilityNote { get; set; } = "";
    public string SubstrateProductId { get; set; } = "";
    public string InsulationProductId { get; set; } = "";
    public string InsulationVariantId { get; set; } = "";
    public string InteriorLiningProductId { get; set; } = "";
    public WallAssembly Assembly { get; set; } = new();
    public List<string> SourceIds { get; set; } = new();
}
