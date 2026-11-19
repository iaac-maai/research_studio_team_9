using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;

namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

/// <summary>
/// Common integration contract for assembly records from different datasets.
/// Source-specific records and source files remain unchanged.
/// </summary>
public sealed class UnifiedAssemblyRecord
{
    public string AssemblyId { get; set; } = "";

    public string AssemblyName { get; set; } = "";

    public string AssemblyFamilyGroup { get; set; } = "";

    public string SubstrateProductId { get; set; } = "";

    public string InsulationProductId { get; set; } = "";

    public string InsulationVariantId { get; set; } = "";

    public string InteriorLiningProductId { get; set; } = "";

    public AssemblyObjectiveRecord Objectives { get; set; } = new();

    public DataProvenance Provenance { get; set; } = new();

    public List<string> ValidationWarnings { get; set; } = new();

    public bool IsStructurallyValid =>
        !string.IsNullOrWhiteSpace(AssemblyId) &&
        !string.IsNullOrWhiteSpace(AssemblyFamilyGroup) &&
        Objectives is not null &&
        Provenance is not null &&
        Provenance.DatasetPartition != DatasetPartition.Unknown;
}
