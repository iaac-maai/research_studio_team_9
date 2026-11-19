using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using SpecSynthAI.UnifiedData.Models;

namespace SpecSynthAI.UnifiedData.Services
{
    public static class ProductCompletenessEvaluator
    {
        public static ProductAssessment Evaluate(UnifiedProductRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var assessment = new ProductAssessment
            {
                ProductId = record.ProductId,
                RecordType = record.RecordType
            };

            JsonObject raw = record.RawJson;
            JsonObject? identification = raw["identification"] as JsonObject;
            JsonObject? classification = raw["classification"] as JsonObject;
            JsonObject? properties = raw["properties"] as JsonObject;
            JsonObject? physical = properties?["physical"] as JsonObject;
            JsonObject? thermal = properties?["thermal"] as JsonObject;
            JsonObject? mechanical = properties?["mechanical"] as JsonObject;
            JsonObject? fire = properties?["fire"] as JsonObject;
            JsonObject? bim = raw["bim_mapping"] as JsonObject;

            // Identity checks
            if (string.IsNullOrWhiteSpace(record.ProductId))
                assessment.IdentityIssues.Add("Product ID is missing.");

            if (string.IsNullOrWhiteSpace(record.ProductName))
                assessment.IdentityIssues.Add("Product name is missing.");

            if (string.IsNullOrWhiteSpace(record.Manufacturer))
                assessment.IdentityIssues.Add("Manufacturer is missing.");

            assessment.IdentityComplete = assessment.IdentityIssues.Count == 0;

            // Product-level technical values. A list of available thicknesses
            // does not count as a selected SKU thickness.
            double? thickness =
                UnifiedProductDatasetLoader.ReadPropertyValue(physical, "thickness_mm");

            double? conductivity =
                UnifiedProductDatasetLoader.ReadPropertyValue(thermal, "thermal_conductivity_W_mK");

            if (!thickness.HasValue || thickness <= 0)
                assessment.TechnicalIssues.Add(
                    "A positive selected product thickness is not available.");

            if (!conductivity.HasValue || conductivity <= 0)
                assessment.TechnicalIssues.Add(
                    "A positive, finite thermal conductivity is not available.");

            assessment.TechnicalComplete = assessment.TechnicalIssues.Count == 0;

            // Optional technical fields are warnings, not blockers for basic
            // thermal calculation.
            double? compressiveStrength =
                UnifiedProductDatasetLoader.ReadPropertyValue(
                    mechanical, "compressive_strength_kPa");

            if (!compressiveStrength.HasValue || compressiveStrength <= 0)
                assessment.TechnicalWarnings.Add(
                    "Compressive strength is missing or not a positive value.");

            string? reactionToFire =
                UnifiedProductDatasetLoader.ReadPropertyText(fire, "reaction_to_fire");

            if (string.IsNullOrWhiteSpace(reactionToFire))
                assessment.TechnicalWarnings.Add(
                    "Reaction-to-fire classification is missing.");

            // Revit import gate remains deliberately stricter than completeness.
            string? revitMaterialName =
                UnifiedProductDatasetLoader.ReadString(bim, "revit_material_name");

            if (string.IsNullOrWhiteSpace(revitMaterialName))
                assessment.ImportBlockers.Add("Revit material mapping is missing.");

            if (record.RecordType != UnifiedRecordType.StandaloneProduct &&
                record.RecordType != UnifiedRecordType.ProductVariant)
            {
                assessment.ImportBlockers.Add(
                    "Only a specific standalone product or resolved variant can be imported.");
            }

            if (!thickness.HasValue || thickness <= 0)
                assessment.ImportBlockers.Add("A positive selected thickness is required.");

            if (!conductivity.HasValue || conductivity <= 0)
                assessment.ImportBlockers.Add("A positive thermal conductivity is required.");

            if (!string.Equals(
                record.VerificationStatus?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase))
            {
                assessment.ImportBlockers.Add(
                    "Record status is not explicitly 'verified'; import approval is not inferred.");
            }

            if (!assessment.IdentityComplete)
                assessment.ImportBlockers.Add("Product identity is incomplete.");

            assessment.IsRevitImportEligible =
                assessment.ImportBlockers.Count == 0;

            return assessment;
        }
    }
}
