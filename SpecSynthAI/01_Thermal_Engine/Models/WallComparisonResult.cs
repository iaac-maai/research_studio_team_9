namespace ThermalEngine.Models;

public class WallComparisonResult
{
    // Assembly identification.
    public string AssemblyName { get; set; } = "";

    // Calculation validity.
    public bool IsValid { get; set; }

    // Material-layer resistance.
    public double? LayerResistance { get; set; }

    // Internal surface resistance (Rsi).
    public double? InternalSurfaceResistance { get; set; }

    // External surface resistance (Rse).
    public double? ExternalSurfaceResistance { get; set; }

    // Complete assembly resistance.
    public double? TotalResistance { get; set; }

    // Overall thermal transmittance.
    public double? UValue { get; set; }

    // Improvement relative to baseline.
    public double? ImprovementPercentage { get; set; }

    // Individual material layer results.
    public List<ThermalLayerResult> LayerResults { get; set; } = new();

    // Validation errors.
    public List<string> Errors { get; set; } = new();
}