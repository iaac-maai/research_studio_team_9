using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.AssemblyOptimisation.Models;

public sealed class ProductRecord
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = "";

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = "";

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("material_family")]
    public string MaterialFamily { get; set; } = "";

    [JsonPropertyName("applications_declared")]
    public List<string> ApplicationsDeclared { get; set; } = new();

    [JsonPropertyName("properties")]
    public Dictionary<string, JsonElement> Properties { get; set; } = new();

    // Variants are attached by ProductDatabaseReader
    // after reading the independent v1.2 variant file.
    [JsonPropertyName("variants")]
    public List<ProductVariant> Variants { get; set; } = new();

    // Retained for compatibility with the earlier v1.1 structure.
    [JsonPropertyName("source_ids")]
    public List<string> SourceIds { get; set; } = new();

    [JsonPropertyName("dataset_role")]
    public string? DatasetRole { get; set; }

    [JsonPropertyName("compatibility_status")]
    public string? CompatibilityStatus { get; set; }

    [JsonPropertyName("record_scope")]
    public string? RecordScope { get; set; }

    [JsonPropertyName("record_status")]
    public string? RecordStatus { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}