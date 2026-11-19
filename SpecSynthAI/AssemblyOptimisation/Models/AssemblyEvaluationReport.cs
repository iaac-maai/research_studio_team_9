using System.Collections.Generic;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Models;

/// <summary>
/// Evaluation output for generated assembly candidates
/// and imported wall assembly datasets.
/// </summary>
public sealed class AssemblyEvaluationReport
{
    // =====================================================
    // EXISTING GENERATED-CANDIDATE PIPELINE
    // =====================================================

    public string DatasetVersion { get; set; } = "";

    public int CandidatesReceived { get; set; }

    public int ValidCalculations { get; set; }

    public int InvalidCalculations { get; set; }

    public string? BestPerformingCandidateId { get; set; }

    public string? BestPerformingAssembly { get; set; }

    public double? LowestUValue { get; set; }

    public WallComparisonReport ComparisonReport { get; set; } = new();

    public List<AssemblyEvaluationItem> RankedCandidates { get; set; } = new();

    public List<string> Warnings { get; set; } = new();


    // =====================================================
    // IMPORTED ASSEMBLY DATASET PIPELINE
    // =====================================================

    public string DatasetId { get; set; } = "";

    public string SchemaVersion { get; set; } = "";

    public int AssembliesReceived { get; set; }

    public int AssembliesReadyForCalculation { get; set; }

    public int AssembliesNotReadyForCalculation { get; set; }

    // Best assembly identification
    public string? BestPerformingAssemblyId { get; set; }

    // All imported assemblies with mapping and calculation results
    public List<AssemblyDatasetEvaluationItem> DatasetAssemblies { get; set; } = new();

    // Assemblies ordered by thermal calculation result
    public List<AssemblyDatasetEvaluationItem> RankedDatasetAssemblies { get; set; } = new();
}