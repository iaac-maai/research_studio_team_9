using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.AssemblyOptimisation.Models;

public sealed class ProductVariant
{
    [JsonPropertyName("variant_id")]
    public string VariantId { get; set; } = "";

    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = "";

    [JsonPropertyName("product_code")]
    public string? ProductCode { get; set; }

    [JsonPropertyName("thickness_mm")]
    public double? ThicknessMm { get; set; }

    [JsonPropertyName("thermal_resistance_m2K_W")]
    public double? ThermalResistanceM2K_W { get; set; }

    [JsonPropertyName("r_status")]
    public string? RStatus { get; set; }

    [JsonPropertyName("thermal_conductivity_W_mK")]
    public double? ThermalConductivityWmK { get; set; }

    [JsonPropertyName("variant_source_status")]
    public string? VariantSourceStatus { get; set; }

    [JsonPropertyName("variant_notes")]
    public string? VariantNotes { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}