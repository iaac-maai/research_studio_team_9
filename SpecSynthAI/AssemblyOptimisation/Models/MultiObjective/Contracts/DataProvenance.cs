namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

/// <summary>
/// Describes the origin, evidence status and restrictions of objective values.
/// </summary>
public sealed class DataProvenance
{
    public DatasetPartition DatasetPartition { get; set; }
        = DatasetPartition.Unknown;

    public string DatasetVersion { get; set; } = "";

    public string TargetProvenance { get; set; } = "";

    public string CarbonProvenance { get; set; } = "";

    public string CostProvenance { get; set; } = "";

    public string VerificationStatus { get; set; } = "";

    public string CompatibilityStatus { get; set; } = "NOT_ASSESSED";

    public bool SyntheticRecord { get; set; }

    public bool PreliminaryRecord { get; set; }

    public List<string> EvidenceIds { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
}
