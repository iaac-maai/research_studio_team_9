using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.AssemblyOptimisation.Models;

public sealed class ProductDatabaseMetadata
{
    [JsonPropertyName("dataset_name")]
    public string DatasetName { get; set; } = "";

    [JsonPropertyName("dataset_version")]
    public string DatasetVersion { get; set; } = "";

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "";

    [JsonPropertyName("unit_system")]
    public string UnitSystem { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("verification_policy")]
    public string VerificationPolicy { get; set; } = "";

    [JsonPropertyName("null_policy")]
    public string NullPolicy { get; set; } = "";

    [JsonPropertyName("ml_policy")]
    public string MlPolicy { get; set; } = "";

    // Preserves additional v1.2 metadata such as:
    // product_count, variant_count, source_count,
    // assembly_count, market_scope and release_status.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}