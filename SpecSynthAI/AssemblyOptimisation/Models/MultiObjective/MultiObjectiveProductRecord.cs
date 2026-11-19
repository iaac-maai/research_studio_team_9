namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;

public sealed class MultiObjectiveProductRecord
{
    public string ProductId { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string MaterialFamily { get; set; } = "";
    public double? ThermalConductivityWmK { get; set; }
    public double? DensityKgM3 { get; set; }
    public double? CompressiveStrengthKPa { get; set; }
    public double? GwpA1A3KgCo2ePerDeclaredUnit { get; set; }
    public string? GwpDeclaredUnit { get; set; }
    public double? PriceEurPerDeclaredUnit { get; set; }
    public string? PriceDeclaredUnit { get; set; }
    public string DataClass { get; set; } = "";
    public bool SyntheticRecord { get; set; }
    public string VerificationStatus { get; set; } = "";
    public List<string> EvidenceIds { get; set; } = new();
}
