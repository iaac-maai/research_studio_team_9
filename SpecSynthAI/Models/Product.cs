using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpecSynthAI.Models
{
    // =========================================================
    // PRODUCT
    // Supports both the existing flat catalogue and the
    // new unified product dataset structure.
    // =========================================================

    public class Product
    {
        // -----------------------------------------------------
        // LEGACY PROPERTIES
        // Preserve compatibility with the existing catalogue.
        // -----------------------------------------------------

        public string ProductId { get; set; } = "";

        public string Manufacturer { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string Category { get; set; } = "";

        public double ThicknessMm { get; set; }

        [JsonPropertyName("thermal_conductivity_W_mK")]
        public double? ThermalConductivityWmK { get; set; }

        public double CompressiveStrengthKPa { get; set; }

        public double CompressiveStrengthToleranceKPa { get; set; }

        public string ReactionToFire { get; set; } = "";

        public string SourceUrl { get; set; } = "";

        public string SourceDocument { get; set; } = "";

        public string VerificationStatus { get; set; } = "";

        public string RevitMaterialName { get; set; } = "";

        public string MaterialFamily { get; set; } = "";

        public List<double> AvailableThicknessesMm { get; set; } = new();


        // -----------------------------------------------------
        // UNIFIED DATASET PROPERTIES
        // -----------------------------------------------------

        public ProductIdentification? Identification { get; set; }

        public ProductClassification? Classification { get; set; }

        [JsonPropertyName("properties")]
        public ProductProperties? ProductProperties { get; set; }

        [JsonPropertyName("available_variants")]
        public List<ProductVariant> AvailableVariants { get; set; } = new();

        public ProductDocumentation? Documentation { get; set; }

        public ProductVerification? Verification { get; set; }

        [JsonPropertyName("bim_mapping")]
        public ProductBimMapping? BimMapping { get; set; }

        [JsonPropertyName("dataset_role")]
        public string DatasetRole { get; set; } = "";
    }


    // =========================================================
    // PRODUCT IDENTIFICATION
    // =========================================================

    public class ProductIdentification
    {
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = "";

        public string Manufacturer { get; set; } = "";

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = "";

        [JsonPropertyName("product_code")]
        public string? ProductCode { get; set; }

        public string? Variant { get; set; }

        public string? Description { get; set; }

        [JsonPropertyName("country_of_manufacture")]
        public string? CountryOfManufacture { get; set; }
    }


    // =========================================================
    // PRODUCT CLASSIFICATION
    // =========================================================

    public class ProductClassification
    {
        public string? Category { get; set; }

        [JsonPropertyName("material_family")]
        public string? MaterialFamily { get; set; }

        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        public List<string> Applications { get; set; } = new();

        [JsonPropertyName("intended_use_standard")]
        public string? IntendedUseStandard { get; set; }

        [JsonPropertyName("un_classification")]
        public string? UnClassification { get; set; }

        [JsonPropertyName("compatible_substrates")]
        public List<string> CompatibleSubstrates { get; set; } = new();

        [JsonPropertyName("known_incompatibilities")]
        public List<string> KnownIncompatibilities { get; set; } = new();
    }


    // =========================================================
    // PRODUCT PROPERTIES
    // =========================================================

    public class ProductProperties
    {
        public Dictionary<string, ProductPropertyValue> Physical { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Thermal { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Moisture { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Acoustic { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Fire { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Mechanical { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Environmental { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Economic { get; set; } = new();

        public Dictionary<string, ProductPropertyValue> Durability { get; set; } = new();
    }


    // =========================================================
    // INDIVIDUAL PROPERTY VALUE
    // Allows numeric, text, Boolean and missing values.
    // =========================================================

    public class ProductPropertyValue
    {
        public object? Value { get; set; }

        public string? Unit { get; set; }

        public string Status { get; set; } = "not_provided";

        [JsonPropertyName("evidence_ids")]
        public List<string> EvidenceIds { get; set; } = new();

        public string? Method { get; set; }

        public string? Conditions { get; set; }
    }


    // =========================================================
    // PRODUCT VARIANTS
    // =========================================================

    public class ProductVariant
    {
        [JsonPropertyName("variant_id")]
        public string VariantId { get; set; } = "";

        [JsonPropertyName("thickness_mm")]
        public double? ThicknessMm { get; set; }

        [JsonPropertyName("declared_thermal_resistance_m2K_W")]
        public double? DeclaredThermalResistanceM2KW { get; set; }

        [JsonPropertyName("product_code")]
        public string? ProductCode { get; set; }

        public string? Notes { get; set; }
    }


    // =========================================================
    // PRODUCT DOCUMENTATION
    // =========================================================

    public class ProductDocumentation
    {
        [JsonPropertyName("primary_product_url")]
        public string? PrimaryProductUrl { get; set; }

        [JsonPropertyName("technical_datasheet_url")]
        public string? TechnicalDatasheetUrl { get; set; }

        [JsonPropertyName("declaration_of_performance_url")]
        public string? DeclarationOfPerformanceUrl { get; set; }

        [JsonPropertyName("epd_url")]
        public string? EpdUrl { get; set; }

        [JsonPropertyName("epd_registration_number")]
        public string? EpdRegistrationNumber { get; set; }

        [JsonPropertyName("epd_valid_until")]
        public string? EpdValidUntil { get; set; }

        public List<string> Documents { get; set; } = new();
    }


    // =========================================================
    // PRODUCT VERIFICATION
    // =========================================================

    public class ProductVerification
    {
        [JsonPropertyName("record_status")]
        public string RecordStatus { get; set; } = "";

        [JsonPropertyName("eligible_for_training")]
        public bool EligibleForTraining { get; set; }

        [JsonPropertyName("eligible_for_optimisation")]
        public bool EligibleForOptimisation { get; set; }

        [JsonPropertyName("last_reviewed")]
        public string? LastReviewed { get; set; }

        public List<ProductEvidence> Evidence { get; set; } = new();

        public List<string> Limitations { get; set; } = new();
    }


    // =========================================================
    // PRODUCT EVIDENCE
    // =========================================================

    public class ProductEvidence
    {
        [JsonPropertyName("evidence_id")]
        public string EvidenceId { get; set; } = "";

        [JsonPropertyName("source_type")]
        public string SourceType { get; set; } = "";

        [JsonPropertyName("source_title")]
        public string? SourceTitle { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("source_page")]
        public string? SourcePage { get; set; }

        [JsonPropertyName("verification_status")]
        public string VerificationStatus { get; set; } = "";

        [JsonPropertyName("supports_properties")]
        public List<string> SupportsProperties { get; set; } = new();

        public string? Notes { get; set; }
    }


    // =========================================================
    // BIM MAPPING
    // =========================================================

    public class ProductBimMapping
    {
        [JsonPropertyName("revit_material_name")]
        public string? RevitMaterialName { get; set; }

        [JsonPropertyName("revit_material_id")]
        public string? RevitMaterialId { get; set; }

        [JsonPropertyName("ifc_material_name")]
        public string? IfcMaterialName { get; set; }

        [JsonPropertyName("ifc_material_classification")]
        public string? IfcMaterialClassification { get; set; }
    }
}