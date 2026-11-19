using System;
using System.Collections.Generic;
using System.Linq;

using ThermalEngine.Models;

namespace SpecSynthAI.Services
{
    /// <summary>
    /// Evaluates the quality and provenance of material inputs
    /// used by the thermal calculation engine.
    ///
    /// This service does not calculate thermal performance.
    /// </summary>
    public class MaterialDataQualityService
    {
        // ============================================
        // MAIN ASSESSMENT METHOD
        // ============================================

        public MaterialDataQualityResult Assess(
            IEnumerable<ThermalLayer> layers)
        {
            ArgumentNullException.ThrowIfNull(layers);

            var layerList = layers.ToList();

            var result = new MaterialDataQualityResult
            {
                TotalLayers = layerList.Count
            };

            if (layerList.Count == 0)
            {
                result.Status = "NO DATA";

                result.Findings.Add(
                    "No material layers were supplied for assessment.");

                return result;
            }

            // Assess each material layer.
            for (int index = 0; index < layerList.Count; index++)
            {
                ThermalLayer layer = layerList[index];

                MaterialLayerQualityResult assessment =
                    AssessLayer(layer, index + 1);

                result.Layers.Add(assessment);
            }

            // ========================================
            // AGGREGATE QUALITY COUNTS
            // ========================================

            result.VerifiedLayers = result.Layers.Count(
                layer => layer.DataStatus == "VERIFIED");

            result.RevitAssetLayers = result.Layers.Count(
                layer => layer.DataStatus == "REVIT DATA");

            result.UnknownSourceLayers = result.Layers.Count(
                layer => layer.DataStatus == "UNKNOWN SOURCE");

            result.MissingConductivityLayers = result.Layers.Count(
                layer => layer.ConductivityStatus == "MISSING");

            result.InvalidConductivityLayers = result.Layers.Count(
                layer => layer.ConductivityStatus == "INVALID");

            result.InvalidThicknessLayers = result.Layers.Count(
                layer => layer.ThicknessStatus == "INVALID");

            result.MissingNameLayers = result.Layers.Count(
                layer => layer.NameStatus == "MISSING");

            result.LayersRequiringReview = result.Layers.Count(
                layer => layer.RequiresReview);

            // ========================================
            // OVERALL QUALITY STATUS
            // ========================================

            bool hasInvalidData =
                result.InvalidConductivityLayers > 0 ||
                result.InvalidThicknessLayers > 0;

            bool hasMissingData =
                result.MissingConductivityLayers > 0 ||
                result.MissingNameLayers > 0;

            bool hasUnverifiedData =
                result.UnknownSourceLayers > 0 ||
                result.RevitAssetLayers > 0;

            if (hasInvalidData)
            {
                result.Status = "INVALID DATA";
            }
            else if (hasMissingData || hasUnverifiedData)
            {
                result.Status = "REQUIRES REVIEW";
            }
            else
            {
                result.Status = "COMPLETE";
            }

            GenerateFindings(result);

            return result;
        }


        // ============================================
        // INDIVIDUAL LAYER ASSESSMENT
        // ============================================

        private MaterialLayerQualityResult AssessLayer(
            ThermalLayer layer,
            int layerNumber)
        {
            var assessment = new MaterialLayerQualityResult
            {
                LayerNumber = layerNumber,
                LayerName = layer.LayerName ?? "",
                ThicknessMm = layer.ThicknessMm,
                ThermalConductivityWmK =
                    layer.ThermalConductivityWmK
            };

            // ========================================
            // 1. MATERIAL NAME
            // ========================================

            if (string.IsNullOrWhiteSpace(layer.LayerName))
            {
                assessment.NameStatus = "MISSING";

                assessment.Findings.Add(
                    "Material layer name is missing.");
            }
            else
            {
                assessment.NameStatus = "VALID";
            }

            // ========================================
            // 2. THICKNESS
            // ========================================

            if (!double.IsFinite(layer.ThicknessMm) ||
                layer.ThicknessMm <= 0)
            {
                assessment.ThicknessStatus = "INVALID";

                assessment.Findings.Add(
                    "Layer thickness is missing, non-finite or non-positive.");
            }
            else
            {
                assessment.ThicknessStatus = "VALID";
            }

            // ========================================
            // 3. THERMAL CONDUCTIVITY
            // ========================================

            if (!layer.ThermalConductivityWmK.HasValue)
            {
                assessment.ConductivityStatus = "MISSING";

                assessment.Findings.Add(
                    "Thermal conductivity is unavailable.");
            }
            else if (
                !double.IsFinite(layer.ThermalConductivityWmK.Value) ||
                layer.ThermalConductivityWmK.Value <= 0)
            {
                assessment.ConductivityStatus = "INVALID";

                assessment.Findings.Add(
                    "Thermal conductivity must be finite and positive.");
            }
            else
            {
                assessment.ConductivityStatus = "VALID";
            }

            // ========================================
            // 4. CONDUCTIVITY PROVENANCE
            // ========================================

            switch (layer.ConductivitySource)
            {
                case ConductivitySource.VerifiedManufacturerProduct:

                    assessment.DataStatus = "VERIFIED";

                    assessment.SourceDescription =
                        "Verified manufacturer product";

                    break;

                case ConductivitySource.RevitThermalAsset:

                    assessment.DataStatus = "REVIT DATA";

                    assessment.SourceDescription =
                        "Revit thermal asset";

                    assessment.Findings.Add(
                        "Conductivity originates from a Revit thermal asset. " +
                        "Manufacturer verification is not established.");

                    break;

                default:

                    assessment.DataStatus = "UNKNOWN SOURCE";

                    assessment.SourceDescription =
                        "Unknown conductivity source";

                    assessment.Findings.Add(
                        "Conductivity provenance cannot be established.");

                    break;
            }

            // ========================================
            // 5. DETERMINE REVIEW REQUIREMENT
            // ========================================

            assessment.RequiresReview =
                assessment.NameStatus == "MISSING" ||
                assessment.ThicknessStatus == "INVALID" ||
                assessment.ConductivityStatus != "VALID" ||
                assessment.DataStatus != "VERIFIED";

            // ========================================
            // 6. GENERATE LAYER SUMMARY
            // ========================================

            if (assessment.RequiresReview)
            {
                assessment.Summary =
                    "This material layer requires data verification.";
            }
            else
            {
                assessment.Summary =
                    "Material inputs are complete and use verified " +
                    "manufacturer conductivity data.";
            }

            return assessment;
        }


        // ============================================
        // ASSEMBLY FINDINGS
        // ============================================

        private void GenerateFindings(
            MaterialDataQualityResult result)
        {
            if (result.MissingConductivityLayers > 0)
            {
                result.Findings.Add(
                    $"{result.MissingConductivityLayers} layer(s) " +
                    "have missing thermal conductivity.");
            }

            if (result.InvalidConductivityLayers > 0)
            {
                result.Findings.Add(
                    $"{result.InvalidConductivityLayers} layer(s) " +
                    "have invalid thermal conductivity.");
            }

            if (result.InvalidThicknessLayers > 0)
            {
                result.Findings.Add(
                    $"{result.InvalidThicknessLayers} layer(s) " +
                    "have invalid thickness.");
            }

            if (result.RevitAssetLayers > 0)
            {
                result.Findings.Add(
                    $"{result.RevitAssetLayers} layer(s) use Revit " +
                    "thermal asset conductivity data.");
            }

            if (result.UnknownSourceLayers > 0)
            {
                result.Findings.Add(
                    $"{result.UnknownSourceLayers} layer(s) have " +
                    "unknown conductivity provenance.");
            }

            if (result.VerifiedLayers > 0)
            {
                result.Findings.Add(
                    $"{result.VerifiedLayers} layer(s) use verified " +
                    "manufacturer conductivity data.");
            }

            if (result.LayersRequiringReview == 0)
            {
                result.Findings.Add(
                    "All material layers have complete inputs and " +
                    "verified manufacturer conductivity provenance.");
            }
        }
    }


    // ============================================
    // ASSEMBLY DATA QUALITY RESULT
    // ============================================

    public class MaterialDataQualityResult
    {
        public string Status { get; set; } = "NOT ANALYSED";

        public int TotalLayers { get; set; }

        public int VerifiedLayers { get; set; }

        public int RevitAssetLayers { get; set; }

        public int UnknownSourceLayers { get; set; }

        public int MissingConductivityLayers { get; set; }

        public int InvalidConductivityLayers { get; set; }

        public int InvalidThicknessLayers { get; set; }

        public int MissingNameLayers { get; set; }

        public int LayersRequiringReview { get; set; }

        public List<MaterialLayerQualityResult> Layers { get; set; }
            = new();

        public List<string> Findings { get; set; }
            = new();
    }


    // ============================================
    // INDIVIDUAL MATERIAL QUALITY RESULT
    // ============================================

    public class MaterialLayerQualityResult
    {
        public int LayerNumber { get; set; }

        public string LayerName { get; set; } = "";

        public double ThicknessMm { get; set; }

        public double? ThermalConductivityWmK { get; set; }

        public string NameStatus { get; set; } = "NOT ANALYSED";

        public string ThicknessStatus { get; set; } = "NOT ANALYSED";

        public string ConductivityStatus { get; set; } = "NOT ANALYSED";

        public string DataStatus { get; set; } = "NOT ANALYSED";

        public string SourceDescription { get; set; } = "";

        public bool RequiresReview { get; set; }

        public string Summary { get; set; } = "";

        public List<string> Findings { get; set; } = new();
    }
}