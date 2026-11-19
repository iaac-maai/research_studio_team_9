using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpecSynthAI.Models
{
    /// <summary>
    /// One document-to-product evidence relationship.
    /// A registry entry is not proof that the document has been reviewed.
    /// </summary>
    public sealed class EvidenceRecord
    {
        [JsonPropertyName("evidence_id")]
        public string EvidenceId { get; set; } = "";

        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = "";

        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; } = "other";

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = "";

        [JsonPropertyName("document_filename")]
        public string? DocumentFilename { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("issue_date")]
        public DateOnly? IssueDate { get; set; }

        [JsonPropertyName("valid_until")]
        public DateOnly? ValidUntil { get; set; }

        [JsonPropertyName("validity_status")]
        public string ValidityStatus { get; set; } = "unknown";

        [JsonPropertyName("supported_properties")]
        public List<string> SupportedProperties { get; set; } = new();

        [JsonPropertyName("verification_status")]
        public string VerificationStatus { get; set; } = "unreviewed";

        [JsonPropertyName("review_notes")]
        public string? ReviewNotes { get; set; }
    }

    public sealed class EvidenceRegistryMetadata
    {
        [JsonPropertyName("registry_name")]
        public string RegistryName { get; set; } = "SpecSynth AI Product Evidence Registry";

        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; } = "1.0.0";

        [JsonPropertyName("registry_version")]
        public string RegistryVersion { get; set; } = "1.0.0";

        [JsonPropertyName("created_at_utc")]
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("updated_at_utc")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }

    public sealed class EvidenceRegistryDocument
    {
        [JsonPropertyName("registry_metadata")]
        public EvidenceRegistryMetadata RegistryMetadata { get; set; } = new();

        [JsonPropertyName("evidence_records")]
        public List<EvidenceRecord> EvidenceRecords { get; set; } = new();
    }

    public sealed class EvidenceValidationResult
    {
        public bool IsStructurallyValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }
}
