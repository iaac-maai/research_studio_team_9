using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpecSynthAI.UnifiedData.Models;

namespace SpecSynthAI.UnifiedData.Services
{
    public static class UnifiedProductDatasetLoader
    {
        public static UnifiedProductDataset Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Dataset path cannot be empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Unified product dataset was not found.", filePath);

            JsonObject root = JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject
                ?? throw new InvalidDataException("Dataset root must be a JSON object.");

            JsonArray products = root["products"] as JsonArray
                ?? throw new InvalidDataException("Dataset must contain a 'products' array.");

            JsonObject? metadata = root["dataset_metadata"] as JsonObject;

            var dataset = new UnifiedProductDataset
            {
                SourcePath = filePath,
                DatasetName = ReadString(metadata, "dataset_name") ?? "Unknown",
                DatasetVersion = ReadString(metadata, "dataset_version") ?? "Unknown"
            };

            foreach (JsonNode? item in products)
            {
                if (item is not JsonObject product)
                    continue;

                JsonObject raw = (JsonObject)product.DeepClone();
                JsonObject? identification = raw["identification"] as JsonObject;
                JsonObject? classification = raw["classification"] as JsonObject;
                JsonObject? verification = raw["verification"] as JsonObject;

                string id = ReadString(identification, "product_id") ?? "";
                string name = ReadString(identification, "product_name") ?? "";
                string manufacturer = ReadString(identification, "manufacturer") ?? "";
                string status = ReadString(verification, "record_status") ?? "";

                string? familyId =
                    ReadString(raw, "family_id") ??
                    ReadString(identification, "family_id");

                string? parentId =
                    ReadString(raw, "parent_product_id") ??
                    ReadString(identification, "parent_product_id");

                UnifiedRecordType recordType = DetectRecordType(
                    raw, identification, classification, familyId, parentId);

                dataset.Products.Add(new UnifiedProductRecord
                {
                    ProductId = id,
                    ProductName = name,
                    Manufacturer = manufacturer,
                    VerificationStatus = status,
                    FamilyId = familyId,
                    ParentProductId = parentId,
                    RecordType = recordType,
                    RawJson = raw
                });
            }

            return dataset;
        }

        private static UnifiedRecordType DetectRecordType(
            JsonObject raw,
            JsonObject? identification,
            JsonObject? classification,
            string? familyId,
            string? parentId)
        {
            string? explicitType =
                ReadString(raw, "record_type") ??
                ReadString(identification, "record_type");

            if (!string.IsNullOrWhiteSpace(explicitType))
            {
                string normalized = explicitType.Trim().ToLowerInvariant()
                    .Replace("-", "_").Replace(" ", "_");

                if (normalized.Contains("family"))
                    return UnifiedRecordType.ProductFamily;
                if (normalized.Contains("variant"))
                    return UnifiedRecordType.ProductVariant;
                if (normalized.Contains("standalone") || normalized == "product")
                    return UnifiedRecordType.StandaloneProduct;
            }

            if (!string.IsNullOrWhiteSpace(parentId))
                return UnifiedRecordType.ProductVariant;

            JsonObject? physical = raw["properties"]?["physical"] as JsonObject;
            double? selectedThickness = ReadPropertyValue(physical, "thickness_mm");
            bool hasVariants = raw["available_variants"] is JsonArray variants &&
                               variants.Count > 0;

            string category = ReadString(classification, "category") ?? "";
            string name = ReadString(identification, "product_name") ?? "";

            if (!string.IsNullOrWhiteSpace(familyId) ||
                category.Contains("family", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("product family", StringComparison.OrdinalIgnoreCase))
                return UnifiedRecordType.ProductFamily;

            // A record with multiple available variants and no selected SKU thickness
            // is a family-level record, not an importable product variant.
            if (hasVariants && (!selectedThickness.HasValue || selectedThickness <= 0))
                return UnifiedRecordType.ProductFamily;

            if (selectedThickness.HasValue && selectedThickness > 0)
                return UnifiedRecordType.StandaloneProduct;

            // An identifiable product remains a standalone product record even
            // when some technical properties have not yet been extracted.
            if (!string.IsNullOrWhiteSpace(name))
                return UnifiedRecordType.StandaloneProduct;

            return UnifiedRecordType.Unknown;
        }

        internal static string? ReadString(JsonObject? obj, string key)
        {
            JsonNode? node = obj?[key];
            if (node == null) return null;

            try { return node.GetValue<string>(); }
            catch { return node.ToString(); }
        }

        internal static double? ReadPropertyValue(JsonObject? group, string propertyName)
        {
            JsonObject? property = group?[propertyName] as JsonObject;
            JsonNode? value = property?["value"];
            if (value == null) return null;

            try
            {
                double number = value.GetValue<double>();
                return double.IsFinite(number) ? number : null;
            }
            catch { return null; }
        }

        internal static string? ReadPropertyText(JsonObject? group, string propertyName)
        {
            JsonObject? property = group?[propertyName] as JsonObject;
            JsonNode? value = property?["value"];
            if (value == null) return null;

            try { return value.GetValue<string>(); }
            catch { return value.ToString(); }
        }
    }
}
