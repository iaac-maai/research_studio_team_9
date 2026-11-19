using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpecSynthAI.Models;

namespace SpecSynthAI.Services
{
    /// <summary>
    /// Reads the nested SpecSynth dataset and produces UI-ready records.
    /// This is diagnostic/reporting logic only. RevitMaterialCreationService
    /// remains the final authority for whether a Product can be imported.
    /// </summary>
    public static class ProductLibraryService
    {
        public static ProductLibraryLoadResult Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Dataset path is required.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Unified product dataset not found.", path);

            JsonObject root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidDataException("Dataset root must be a JSON object.");

            JsonArray products = root["products"] as JsonArray
                ?? root["records"] as JsonArray
                ?? throw new InvalidDataException("Dataset must contain a 'products' or 'records' array.");

            JsonObject? metadata = root["dataset_metadata"] as JsonObject
                ?? root["metadata"] as JsonObject;

            var result = new ProductLibraryLoadResult
            {
                DatasetName = ReadString(metadata, "dataset_name") ?? "Unknown dataset",
                DatasetVersion = ReadString(metadata, "dataset_version")
                    ?? ReadString(metadata, "schema_version") ?? "Unknown",
                SourcePath = path
            };

            foreach (JsonNode? node in products)
            {
                if (node is JsonObject obj)
                    result.Records.Add(BuildRecord(obj));
            }

            result.TotalProducts = result.Records.Count;
            result.Incomplete = result.Records.Count(r => r.CompletenessIssues.Count > 0);
            result.ReadyForImport = result.Records.Count(r => r.ImportIssues.Count == 0);
            result.NotVerifiedOrBlocked = result.Records.Count(r =>
                r.CompletenessIssues.Count == 0 && r.ImportIssues.Count > 0);
            return result;
        }

        public static string FormatFullRecord(MaterialLibraryRecord record) =>
            record.RawJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        /// <summary>
        /// Re-evaluates import diagnostics for the selected record. A catalogue
        /// variant selection is never promoted into an independently verified SKU.
        /// </summary>
        public static List<ImportDiagnostic> EvaluateImportDiagnostics(
            MaterialLibraryRecord record,
            MaterialLibraryVariant? selectedVariant = null)
        {
            var diagnostics = new List<ImportDiagnostic>();

            if (string.IsNullOrWhiteSpace(record.ProductId))
                Add(diagnostics, "Identity", "BLOCKED", "Product ID is missing.");
            if (string.IsNullOrWhiteSpace(record.ProductName))
                Add(diagnostics, "Identity", "BLOCKED", "Product name is missing.");
            if (string.IsNullOrWhiteSpace(record.Manufacturer))
                Add(diagnostics, "Identity", "BLOCKED", "Manufacturer is missing.");
            if (string.IsNullOrWhiteSpace(record.Category))
                Add(diagnostics, "Classification", "BLOCKED", "Product category is missing.");

            if (!IsPositiveFinite(record.ThicknessMm))
                Add(diagnostics, "Technical data", "BLOCKED", "A positive finite product thickness is not supplied in the record.");
            if (!IsPositiveFinite(record.ThermalConductivityWmK))
                Add(diagnostics, "Technical data", "BLOCKED", "A positive finite thermal conductivity is not supplied in the record.");

            if (!string.Equals(record.RecordStatus, "verified", StringComparison.OrdinalIgnoreCase))
                Add(diagnostics, "Manufacturer verification", "BLOCKED",
                    $"Dataset status is '{(string.IsNullOrWhiteSpace(record.RecordStatus) ? "not supplied" : record.RecordStatus)}'; explicit 'verified' status is required.");

            if (record.RecordType == "ProductFamily")
                Add(diagnostics, "SKU / variant", "BLOCKED",
                    "This is a family-level record. A separately verified SKU-specific product record is required.");

            if (selectedVariant != null)
                Add(diagnostics, "SKU / variant", "BLOCKED",
                    $"{selectedVariant.VariantId} is selected from catalogue availability only; the selection does not establish SKU-specific verification or technical evidence.");

            if (string.IsNullOrWhiteSpace(record.RevitMaterialName))
                Add(diagnostics, "Revit material mapping", "BLOCKED",
                    "No Revit material name is mapped to this product record.");

            // Keep the existing Revit service as the final import authority.
            MaterialImportValidationResult finalValidation =
                RevitMaterialCreationService.ValidateProduct(record.ImportProduct);
            foreach (string error in finalValidation.Errors ?? new List<string>())
                Add(diagnostics, "Revit importer validation", "BLOCKED", error);

            return Deduplicate(diagnostics);
        }

        private static MaterialLibraryRecord BuildRecord(JsonObject raw)
        {
            JsonObject identification = Section(raw, "identification");
            JsonObject classification = Section(raw, "classification");
            JsonObject properties = Section(raw, "properties");
            JsonObject physical = Section(properties, "physical");
            JsonObject thermal = Section(properties, "thermal");
            JsonObject fire = Section(properties, "fire");
            JsonObject mechanical = Section(properties, "mechanical");
            JsonObject verification = Section(raw, "verification");
            JsonObject bim = Section(raw, "bim_mapping");

            string id = ReadString(identification, "product_id") ?? "";
            string name = ReadString(identification, "product_name") ?? "";
            string manufacturer = ReadString(identification, "manufacturer") ?? "";
            string category = ReadString(classification, "category") ?? "";
            string status = ReadString(verification, "record_status") ?? "";
            double? thickness = ReadPropertyNumber(physical, "thickness_mm");
            double? conductivity = ReadPropertyNumber(thermal, "thermal_conductivity_W_mK");
            string? fireRating = ReadPropertyString(fire, "reaction_to_fire");
            double? compressive = ReadPropertyNumber(mechanical, "compressive_strength_kPa");
            string? revitName = ReadString(bim, "revit_material_name");
            string? sourceUrl = ReadString(Section(raw, "documentation"), "primary_product_url");
            string? sourceDocument = ReadString(Section(raw, "documentation"), "technical_datasheet_url");
            string family = ReadString(classification, "material_family") ?? "";

            var importProduct = new Product
            {
                ProductId = id,
                ProductName = name,
                Manufacturer = manufacturer,
                Category = category,
                ThicknessMm = thickness ?? 0,
                ThermalConductivityWmK = conductivity,
                CompressiveStrengthKPa = compressive ?? 0,
                ReactionToFire = fireRating ?? "",
                VerificationStatus = status,
                RevitMaterialName = revitName ?? "",
                SourceUrl = sourceUrl ?? "",
                SourceDocument = sourceDocument ?? "",
                MaterialFamily = family,
                AvailableThicknessesMm = ReadNumberArray(physical, "available_thicknesses_mm")
            };

            var record = new MaterialLibraryRecord
            {
                ProductId = id,
                ProductName = string.IsNullOrWhiteSpace(name) ? "(Unnamed product)" : name,
                Manufacturer = manufacturer,
                Category = category,
                RecordStatus = status,
                RecordType = DetermineRecordType(raw, physical, thickness),
                RevitMaterialName = revitName ?? "",
                ThicknessMm = thickness,
                ThermalConductivityWmK = conductivity,
                ImportProduct = importProduct,
                RawJson = (JsonObject)raw.DeepClone()
            };

            if (string.IsNullOrWhiteSpace(id)) record.CompletenessIssues.Add("Product ID is missing.");
            if (string.IsNullOrWhiteSpace(name)) record.CompletenessIssues.Add("Product name is missing.");
            if (string.IsNullOrWhiteSpace(manufacturer)) record.CompletenessIssues.Add("Manufacturer is missing.");
            if (!IsPositiveFinite(thickness)) record.CompletenessIssues.Add("A selected positive product thickness is missing.");
            if (!IsPositiveFinite(conductivity)) record.CompletenessIssues.Add("A positive finite thermal conductivity is missing.");

            record.AvailableVariants = ReadVariants(raw);
            record.ImportIssues.AddRange(EvaluateImportDiagnostics(record).Select(d => d.Message));
            return record;
        }

        private static void Add(List<ImportDiagnostic> list, string group, string state, string message) =>
            list.Add(new ImportDiagnostic { Group = group, State = state, Message = message });

        private static List<ImportDiagnostic> Deduplicate(List<ImportDiagnostic> items) =>
            items.GroupBy(x => x.Group + "|" + x.Message, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()).ToList();

        private static string DetermineRecordType(JsonObject raw, JsonObject physical, double? thickness)
        {
            string explicitType = ReadString(raw, "record_type") ?? "";
            if (explicitType.Equals("product_variant", StringComparison.OrdinalIgnoreCase)) return "ProductVariant";
            if (explicitType.Equals("standalone_product", StringComparison.OrdinalIgnoreCase)) return "StandaloneProduct";
            if (explicitType.Equals("product_family", StringComparison.OrdinalIgnoreCase)) return "ProductFamily";
            JsonArray variants = raw["available_variants"] as JsonArray ?? new JsonArray();
            if ((!IsPositiveFinite(thickness)) &&
                (variants.Count > 0 || ReadNumberArray(physical, "available_thicknesses_mm").Count > 0))
                return "ProductFamily";
            return IsPositiveFinite(thickness) ? "StandaloneProduct" : "Unknown";
        }

        private static List<MaterialLibraryVariant> ReadVariants(JsonObject raw)
        {
            var list = new List<MaterialLibraryVariant>();
            if (raw["available_variants"] is not JsonArray array) return list;
            foreach (JsonNode? item in array)
            {
                if (item is not JsonObject variant) continue;
                string id = ReadString(variant, "variant_id") ?? "";
                double? mm = ReadNumber(variant["thickness_mm"]);
                if (string.IsNullOrWhiteSpace(id) || !IsPositiveFinite(mm)) continue;
                list.Add(new MaterialLibraryVariant
                {
                    VariantId = id,
                    ThicknessMm = mm!.Value,
                    ProductCode = ReadString(variant, "product_code") ?? ""
                });
            }
            return list.OrderBy(x => x.ThicknessMm).ToList();
        }

        private static JsonObject Section(JsonObject? parent, string name) => parent?[name] as JsonObject ?? new JsonObject();
        private static string? ReadString(JsonObject? obj, string key)
        {
            JsonNode? node = obj?[key];
            if (node == null) return null;
            if (node is JsonValue value && value.TryGetValue<string>(out string? text)) return text;
            return node.ToString();
        }
        private static double? ReadPropertyNumber(JsonObject obj, string key) => ReadNumber((obj[key] as JsonObject)?["value"]);
        private static string? ReadPropertyString(JsonObject obj, string key) => ReadString(obj[key] as JsonObject, "value");
        private static double? ReadNumber(JsonNode? node)
        {
            if (node == null) return null;
            try
            {
                if (node is JsonValue value && value.TryGetValue<double>(out double number)) return number;
                return double.Parse(node.ToString(), CultureInfo.InvariantCulture);
            }
            catch { return null; }
        }
        private static List<double> ReadNumberArray(JsonObject obj, string key)
        {
            var values = new List<double>();
            if ((obj[key] as JsonObject)?["value"] is not JsonArray array) return values;
            foreach (JsonNode? node in array)
            {
                double? value = ReadNumber(node);
                if (IsPositiveFinite(value)) values.Add(value!.Value);
            }
            return values;
        }
        private static bool IsPositiveFinite(double? value) =>
            value.HasValue && value.Value > 0 && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
    }

    public sealed class ProductLibraryLoadResult
    {
        public string DatasetName { get; set; } = "";
        public string DatasetVersion { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public int TotalProducts { get; set; }
        public int ReadyForImport { get; set; }
        public int Incomplete { get; set; }
        public int NotVerifiedOrBlocked { get; set; }
        public List<MaterialLibraryRecord> Records { get; } = new();
    }

    public sealed class MaterialLibraryRecord
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Category { get; set; } = "";
        public string RecordStatus { get; set; } = "";
        public string RecordType { get; set; } = "";
        public string RevitMaterialName { get; set; } = "";
        public double? ThicknessMm { get; set; }
        public double? ThermalConductivityWmK { get; set; }
        public Product ImportProduct { get; set; } = new();
        public JsonObject RawJson { get; set; } = new();
        public List<string> CompletenessIssues { get; } = new();
        public List<string> ImportIssues { get; } = new();
        public List<MaterialLibraryVariant> AvailableVariants { get; set; } = new();
    }

    public sealed class MaterialLibraryVariant
    {
        public string VariantId { get; set; } = "";
        public double ThicknessMm { get; set; }
        public string ProductCode { get; set; } = "";
        public string Display => $"{ThicknessMm:0.##} mm — {VariantId}";
    }

    public sealed class ImportDiagnostic
    {
        public string Group { get; set; } = "";
        public string State { get; set; } = "BLOCKED";
        public string Message { get; set; } = "";
    }
}
