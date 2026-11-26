namespace ThermalEngine.Models;

public class ThermalCalculationResult
{
    public bool IsValid { get; set; }
    public double? LayerResistance { get; set; }
    public double? InternalSurfaceResistance { get; set; }
    public double? ExternalSurfaceResistance { get; set; }
    public double? TotalResistance { get; set; }
    public double? UValue { get; set; }
    public string ResistanceSelectionSummary { get; set; } = "";

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ThermalLayerResult> LayerResults { get; set; } = new();
}
