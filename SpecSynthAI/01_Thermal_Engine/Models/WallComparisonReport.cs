namespace ThermalEngine.Models;

public class WallComparisonReport
{
    // All compared wall assemblies.
    public List<WallComparisonResult> Results { get; set; } = new();

    // Name of the best-performing valid assembly.
    public string? BestPerformingAssembly { get; set; }

    // Lowest calculated U-value.
    public double? LowestUValue { get; set; }

    // Name of the baseline assembly used for comparison.
    public string? BaselineAssembly { get; set; }

    // Baseline U-value.
    public double? BaselineUValue { get; set; }
}