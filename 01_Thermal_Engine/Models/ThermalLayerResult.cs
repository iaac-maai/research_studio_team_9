using System.Collections.Generic;

namespace ThermalEngine.Models;

public class ThermalLayerResult
{
    // Basic calculation results

    public string LayerName { get; set; } = "";

    public double ThicknessMm { get; set; }

    public double ThermalConductivityWmK { get; set; }

    public double ThermalResistance { get; set; }

    public double ResistanceContributionPercentage { get; set; }

    // Product identification

    public string? ProductId { get; set; }

    public string? VariantId { get; set; }

    public string? ProductCode { get; set; }

    // Conductivity provenance

    public string ConductivitySource { get; set; } = "Unknown";

    public string? ConductivityStatus { get; set; }

    public string? ConductivityEvidenceId { get; set; }

    // Source evidence

    public List<string> SourceIds { get; set; } = new();

    public string? ResistanceEvidence { get; set; }

    public string ResistanceScope { get; set; } = "Unknown";

    // Resistance provenance

    public double? ManufacturerDeclaredResistance { get; set; }

    public double? SelectedResistance { get; set; }

    public string? ResistanceStatus { get; set; }

    public string ResistanceSelectionMethod { get; set; }
        = "CalculatedFromThicknessAndConductivity";

    // Validation

    public double? DiscrepancyPercentage { get; set; }
}