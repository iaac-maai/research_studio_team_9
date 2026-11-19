using System.Collections.Generic;

namespace SpecSynthAI.AssemblyOptimisation.Models.ML;

/// <summary>
/// Represents one row in the SpecSynth AI thermal ML dataset.
///
/// U-value targets are calculated by ThermalEngine.
/// They are not measured building performance data.
///
/// The model also preserves dataset provenance and training restrictions.
/// </summary>
public sealed class AssemblyMLRecord
{
    // ---------------------------------------------------------
    // 1. RECORD IDENTIFICATION
    // ---------------------------------------------------------

    public string RecordId { get; set; } = "";

    public string CandidateId { get; set; } = "";

    public string AssemblyName { get; set; } = "";

    public string CandidateStatus { get; set; } = "";

    // Group key for leakage-resistant train/test splitting.
    public string AssemblyFamilyGroup { get; set; } = "";


    // ---------------------------------------------------------
    // 2. PRODUCT IDENTIFICATION
    // ---------------------------------------------------------

    public string SubstrateProductId { get; set; } = "";

    public string InsulationProductId { get; set; } = "";

    public string InsulationVariantId { get; set; } = "";

    public string InteriorLiningProductId { get; set; } = "";


    // ---------------------------------------------------------
    // 3. ASSEMBLY PROPERTIES
    // ---------------------------------------------------------

    public int NumberOfLayers { get; set; }

    public double TotalThicknessMm { get; set; }

    public double? MinConductivityWmK { get; set; }

    public double? MaxConductivityWmK { get; set; }

    public double? MeanConductivityWmK { get; set; }

    public double? InsulationThicknessMm { get; set; }


    // ---------------------------------------------------------
    // 4. INDIVIDUAL LAYER FEATURES
    // ---------------------------------------------------------

    // Fixed slots for the current three-layer generator.
    // Null means that the relevant property is unavailable.

    public double? Layer1ThicknessMm { get; set; }

    public double? Layer1ConductivityWmK { get; set; }

    public double? Layer2ThicknessMm { get; set; }

    public double? Layer2ConductivityWmK { get; set; }

    public double? Layer3ThicknessMm { get; set; }

    public double? Layer3ConductivityWmK { get; set; }


    // ---------------------------------------------------------
    // 5. THERMAL PERFORMANCE
    // ---------------------------------------------------------

    public double? TotalResistanceM2K_W { get; set; }

    public double? UValue_Wm2K { get; set; }

    public bool IsThermalCalculationValid { get; set; }

    public bool HasCompleteConductivityFeatures { get; set; }


    // ---------------------------------------------------------
    // 6. THERMAL DATA PROVENANCE
    // ---------------------------------------------------------

    public string ConductivityProvenanceSummary { get; set; } = "";

    public string ResistanceScopeSummary { get; set; } = "";

    // Origin of the calculated target.
    public string TargetProvenance { get; set; } = "";

    // Dataset usage restriction.
    public string TrainingUse { get; set; } = "";


    // ---------------------------------------------------------
    // 7. SOURCE REFERENCES AND QUALITY
    // ---------------------------------------------------------

    public List<string> SourceIds { get; set; } = new();

    public List<string> QualityWarnings { get; set; } = new();
}