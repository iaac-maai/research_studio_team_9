using System.Collections.Generic;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Models;

/// <summary>
/// Connects an assembly dataset record with its mapping and thermal result.
/// </summary>
public sealed class AssemblyDatasetEvaluationItem
{
    public string AssemblyId { get; set; } = "";
    public string AssemblyName { get; set; } = "";

    // Mapping status
    public bool IsMappingReady { get; set; }
    public List<string> MappingErrors { get; set; } = new();
    public List<string> MappingWarnings { get; set; } = new();

    // Thermal calculation status
    public bool HasValidThermalResult { get; set; }
    public double? UValue { get; set; }

    public WallComparisonResult ThermalResult { get; set; } = new();
}