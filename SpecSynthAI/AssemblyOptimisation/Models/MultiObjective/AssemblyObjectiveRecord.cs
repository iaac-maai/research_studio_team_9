namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;

public sealed class AssemblyObjectiveRecord
{
    public string AssemblyId { get; set; } = "";
    public string AssemblyFamilyGroup { get; set; } = "";
    public double? UValueWm2K { get; set; }
    public double? EmbodiedCarbonA1A3KgCo2eM2 { get; set; }
    public double? MaterialCostEurM2 { get; set; }
    public double? TotalThicknessMm { get; set; }
    public string TargetProvenance { get; set; } = "";
    public string CarbonProvenance { get; set; } = "";
    public string CostProvenance { get; set; } = "";
    public bool SyntheticRecord { get; set; }
    public string CompatibilityStatus { get; set; } = "NOT_ASSESSED";
}
