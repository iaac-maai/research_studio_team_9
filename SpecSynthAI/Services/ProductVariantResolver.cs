using System;
using System.Linq;
using System.Text.Json.Nodes;
using SpecSynthAI.UnifiedData.Models;

namespace SpecSynthAI.UnifiedData.Services
{
    /// <summary>
    /// Creates a separate, SKU-level variant record from a product-family record.
    /// The original family record is never modified.
    /// </summary>
    public static class ProductVariantResolver
    {
        public static UnifiedProductRecord ResolveVariant(
            UnifiedProductRecord family,
            string variantId)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            if (family.RecordType != UnifiedRecordType.ProductFamily)
                throw new InvalidOperationException(
                    "Only a ProductFamily record can be resolved into a variant.");

            if (string.IsNullOrWhiteSpace(variantId))
                throw new ArgumentException(
                    "A variant ID is required.", nameof(variantId));

            JsonObject raw = (JsonObject)family.RawJson.DeepClone();

            if (raw["available_variants"] is not JsonArray variants)
                throw new InvalidOperationException(
                    "This product family has no available_variants array.");

            JsonObject? selected = variants
                .OfType<JsonObject>()
                .FirstOrDefault(v =>
                    string.Equals(
                        ReadString(v, "variant_id"),
                        variantId,
                        StringComparison.OrdinalIgnoreCase));

            if (selected == null)
                throw new InvalidOperationException(
                    $"Variant '{variantId}' was not found in family '{family.ProductId}'.");

            double thickness;
            try
            {
                thickness = selected["thickness_mm"]!.GetValue<double>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Variant '{variantId}' does not contain a valid thickness.");
            }

            if (!double.IsFinite(thickness) || thickness <= 0)
                throw new InvalidOperationException(
                    $"Variant '{variantId}' has an invalid thickness.");

            JsonObject identification =
                raw["identification"] as JsonObject ?? new JsonObject();

            raw["identification"] = identification;

            identification["product_id"] = variantId;
            identification["variant"] = $"{thickness:0.##} mm";
            identification["product_name"] =
                $"{family.ProductName} - {thickness:0.##} mm";

            raw["record_type"] = "product_variant";
            raw["parent_product_id"] = family.ProductId;
            raw["family_id"] = family.FamilyId ?? family.ProductId;

            // Set the selected SKU thickness while retaining the existing
            // property-object structure used by schema 1.0.0.
            JsonObject properties =
                raw["properties"] as JsonObject ?? new JsonObject();

            raw["properties"] = properties;

            JsonObject physical =
                properties["physical"] as JsonObject ?? new JsonObject();

            properties["physical"] = physical;

            JsonArray evidenceIds = new JsonArray();

            physical["thickness_mm"] = new JsonObject
            {
                ["value"] = JsonValue.Create(thickness),
                ["unit"] = "mm",
                ["status"] = "selected_from_available_variants",
                ["evidence_ids"] = evidenceIds,
                ["method"] = null,
                ["conditions"] =
                    "Thickness selected from the family's available_variants list. " +
                    "Confirm SKU-specific documentation before procurement."
            };

            // Keep available_variants on the cloned record for traceability.
            // Do not change verification status or invent SKU-specific evidence.
            return new UnifiedProductRecord
            {
                ProductId = variantId,
                ProductName = $"{family.ProductName} - {thickness:0.##} mm",
                Manufacturer = family.Manufacturer,
                RecordType = UnifiedRecordType.ProductVariant,
                FamilyId = family.FamilyId ?? family.ProductId,
                ParentProductId = family.ProductId,
                VerificationStatus = family.VerificationStatus,
                RawJson = raw
            };
        }

        private static string? ReadString(JsonObject obj, string key)
        {
            JsonNode? node = obj[key];
            if (node == null) return null;

            try { return node.GetValue<string>(); }
            catch { return node.ToString(); }
        }
    }
}
