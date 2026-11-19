namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;

/// <summary>
/// Represents one synthetic multi-objective assembly simulation.
///
/// This model preserves the original 27 CSV fields.
///
/// Synthetic carbon, cost and thermal values must not be
/// interpreted as verified manufacturer or measured data.
/// </summary>
public sealed class SyntheticAssemblyScenarioRecord
{
    // ---------------------------------------------------------
    // 1. SIMULATION IDENTIFICATION
    // ---------------------------------------------------------

    public string SimulationId { get; set; } = "";

    public string AssemblyFamilyGroup { get; set; } = "";


    // ---------------------------------------------------------
    // 2. SUBSTRATE
    // ---------------------------------------------------------

    public string SubstrateId { get; set; } = "";

    public double SubstrateResistanceM2K_W { get; set; }

    public string SubstrateResistanceScope { get; set; } = "";

    public double SubstrateThicknessMm { get; set; }


    // ---------------------------------------------------------
    // 3. INSULATION
    // ---------------------------------------------------------

    public string InsulationProductId { get; set; } = "";

    public string InsulationMaterialFamily { get; set; } = "";

    public double InsulationThicknessMm { get; set; }

    public double InsulationLambdaWmK { get; set; }


    // ---------------------------------------------------------
    // 4. INTERIOR LINING
    // ---------------------------------------------------------

    public string LiningId { get; set; } = "";

    public double LiningThicknessMm { get; set; }

    public double LiningLambdaWmK { get; set; }


    // ---------------------------------------------------------
    // 5. ASSEMBLY PROPERTIES
    // ---------------------------------------------------------

    public double TotalThicknessMm { get; set; }

    public double RTotalM2K_W { get; set; }


    // ---------------------------------------------------------
    // 6. MULTI-OBJECTIVE VALUES
    // ---------------------------------------------------------

    public double UValueWm2K { get; set; }

    public double EmbodiedCarbonA1A3KgCo2eM2 { get; set; }

    public double MaterialCostEurM2 { get; set; }


    // ---------------------------------------------------------
    // 7. ADDITIONAL MATERIAL PROPERTIES
    // ---------------------------------------------------------

    public double CompressiveStrengthKpa { get; set; }

    public string ReactionToFirePlaceholder { get; set; } = "";


    // ---------------------------------------------------------
    // 8. DATA CLASSIFICATION
    // ---------------------------------------------------------

    public string DataClass { get; set; } = "";

    public bool SyntheticRecord { get; set; }


    // ---------------------------------------------------------
    // 9. PROVENANCE
    // ---------------------------------------------------------

    public string TargetProvenance { get; set; } = "";

    public string CarbonProvenance { get; set; } = "";

    public string CostProvenance { get; set; } = "";


    // ---------------------------------------------------------
    // 10. COMPATIBILITY AND USAGE RESTRICTIONS
    // ---------------------------------------------------------

    public string CompatibilityStatus { get; set; } = "";

    public string UseRestriction { get; set; } = "";
}