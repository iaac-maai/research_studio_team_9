using System.Collections.Generic;

namespace ThermalEngine.Models;

public enum ConductivitySource
{
    Unknown,
    VerifiedManufacturerProduct,
    RevitThermalAsset
}

public enum ResistanceValueScope
{
    Unknown,
    ProductVariant,
    EquivalentWallOrAssembly
}

public class ThermalLayer
{
    // Basic layer information

    public string LayerName { get; set; } = "";

    public double ThicknessMm { get; set; }

    public double? ThermalConductivityWmK { get; set; }

    public ConductivitySource ConductivitySource { get; set; }
        = ConductivitySource.Unknown;

    // Conductivity provenance

    public string? ConductivityStatus { get; set; }

    public string? ConductivityEvidenceId { get; set; }

    // Product identification

    public string? ProductId { get; set; }

    public string? VariantId { get; set; }

    public string? ProductCode { get; set; }

    // Source evidence

    public List<string> SourceIds { get; set; } = new();

    public string? ResistanceEvidence { get; set; }

    public ResistanceValueScope ResistanceScope { get; set; }
        = ResistanceValueScope.Unknown;

    // Manufacturer-declared resistance

    public double? ManufacturerDeclaredResistance { get; set; }

    public string? ResistanceStatus { get; set; }

    // Selected resistance

    public double? SelectedResistance { get; set; }

    public string ResistanceSelectionMethod { get; set; }
        = "CalculatedFromThicknessAndConductivity";
}