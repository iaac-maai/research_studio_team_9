using System;
using System.Collections.Generic;
using System.Linq;

using ThermalEngine.Models;

namespace SpecSynthAI.Services
{
    /// <summary>
    /// Analyses existing thermal calculation results and generates
    /// layer-level diagnostic information.
    ///
    /// This service does not perform thermal calculations.
    /// It interprets results produced by ThermalCalculator.
    /// </summary>
    public class LayerDiagnosticService
    {
        // ============================================
        // MAIN DIAGNOSTIC METHOD
        // ============================================

        public LayerDiagnosticResult Diagnose(
            WallAssembly assembly,
            ThermalCalculationResult calculation)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(calculation);

            var diagnostic = new LayerDiagnosticResult
            {
                AssemblyName = assembly.AssemblyName,
                IsCalculationValid = calculation.IsValid,
                UValue = calculation.UValue,
                TotalResistance = calculation.TotalResistance
            };

            // ----------------------------------------
            // 1. Check calculation validity
            // ----------------------------------------

            if (!calculation.IsValid)
            {
                diagnostic.Status = "NOT ANALYSED";

                diagnostic.Summary =
                    "Layer diagnostics cannot be generated because " +
                    "the thermal calculation is invalid.";

                diagnostic.Findings.AddRange(
                    calculation.Errors);

                return diagnostic;
            }

            if (assembly.Layers.Count == 0 ||
                calculation.LayerResults.Count == 0)
            {
                diagnostic.Status = "NOT ANALYSED";

                diagnostic.Summary =
                    "No material-layer calculation results are available.";

                return diagnostic;
            }

            // ----------------------------------------
            // 2. Analyse individual layers
            // ----------------------------------------

            int layerCount = Math.Min(
                assembly.Layers.Count,
                calculation.LayerResults.Count);

            for (int index = 0; index < layerCount; index++)
            {
                ThermalLayer sourceLayer =
                    assembly.Layers[index];

                ThermalLayerResult calculatedLayer =
                    calculation.LayerResults[index];

                var layerDiagnostic = DiagnoseLayer(
                    sourceLayer,
                    calculatedLayer,
                    index + 1);

                diagnostic.Layers.Add(layerDiagnostic);
            }

            // ----------------------------------------
            // 3. Identify dominant resistance layer
            // ----------------------------------------

            diagnostic.DominantLayer =
                diagnostic.Layers
                    .OrderByDescending(
                        layer => layer.ResistanceContributionPercentage)
                    .FirstOrDefault();

            // ----------------------------------------
            // 4. Generate assembly-level findings
            // ----------------------------------------

            GenerateAssemblyFindings(diagnostic);

            // ----------------------------------------
            // 5. Generate summary
            // ----------------------------------------

            GenerateSummary(diagnostic);

            diagnostic.Status = "ANALYSED";

            return diagnostic;
        }


        // ============================================
        // INDIVIDUAL LAYER DIAGNOSTIC
        // ============================================

        private LayerDiagnosticItem DiagnoseLayer(
            ThermalLayer sourceLayer,
            ThermalLayerResult calculatedLayer,
            int layerNumber)
        {
            var diagnostic = new LayerDiagnosticItem
            {
                LayerNumber = layerNumber,

                LayerName = calculatedLayer.LayerName,

                ThicknessMm = calculatedLayer.ThicknessMm,

                ThermalConductivityWmK =
                    calculatedLayer.ThermalConductivityWmK,

                ThermalResistance =
                    calculatedLayer.ThermalResistance,

                ResistanceContributionPercentage =
                    calculatedLayer.ResistanceContributionPercentage,

                ConductivitySource =
                    sourceLayer.ConductivitySource.ToString()
            };

            // ----------------------------------------
            // 1. Conductivity provenance
            // ----------------------------------------

            switch (sourceLayer.ConductivitySource)
            {
                case ConductivitySource.VerifiedManufacturerProduct:

                    diagnostic.ConductivitySourceDescription =
                        "Verified manufacturer product";

                    diagnostic.DataConfidence = "VERIFIED";

                    break;


                case ConductivitySource.RevitThermalAsset:

                    diagnostic.ConductivitySourceDescription =
                        "Revit thermal asset";

                    diagnostic.DataConfidence = "REVIT DATA";

                    diagnostic.Findings.Add(
                        "Conductivity was obtained from a Revit thermal asset.");

                    break;


                default:

                    diagnostic.ConductivitySourceDescription =
                        "Unknown or unverified source";

                    diagnostic.DataConfidence = "UNVERIFIED";

                    diagnostic.Findings.Add(
                        "Conductivity provenance is unknown or unverified.");

                    break;
            }

            // ----------------------------------------
            // 2. Resistance contribution interpretation
            // ----------------------------------------

            double contribution =
                calculatedLayer.ResistanceContributionPercentage;

            if (contribution >= 50)
            {
                diagnostic.ContributionCategory =
                    "DOMINANT";

                diagnostic.Explanation =
                    "This layer provides the largest contribution " +
                    "to the assembly's thermal resistance.";
            }
            else if (contribution >= 20)
            {
                diagnostic.ContributionCategory =
                    "SIGNIFICANT";

                diagnostic.Explanation =
                    "This layer makes a significant contribution " +
                    "to the assembly's thermal resistance.";
            }
            else
            {
                diagnostic.ContributionCategory =
                    "LIMITED";

                diagnostic.Explanation =
                    "This layer contributes a smaller proportion " +
                    "of the assembly's total thermal resistance.";
            }

            // ----------------------------------------
            // 3. Conductivity interpretation
            // ----------------------------------------

            if (calculatedLayer.ThermalConductivityWmK is double conductivity &&
                double.IsFinite(conductivity) &&
                conductivity > 0)
            {
                diagnostic.ConductivityExplanation =
                    conductivity <= 0.05
                        ? "Relatively low thermal conductivity."
                        : conductivity <= 0.20
                            ? "Moderate thermal conductivity."
                            : "Relatively high thermal conductivity.";
            }
            else
            {
                diagnostic.ConductivityExplanation =
                    "Conductivity value is unavailable or invalid.";

                diagnostic.Findings.Add(
                    "The layer conductivity requires verification.");
            }

            // ----------------------------------------
            // 4. Validate contribution value
            // ----------------------------------------

            if (!double.IsFinite(contribution) ||
                contribution < 0)
            {
                diagnostic.Findings.Add(
                    "Resistance contribution is invalid.");
            }

            return diagnostic;
        }


        // ============================================
        // ASSEMBLY-LEVEL FINDINGS
        // ============================================

        private void GenerateAssemblyFindings(
            LayerDiagnosticResult diagnostic)
        {
            if (diagnostic.DominantLayer == null)
                return;

            var dominantLayer = diagnostic.DominantLayer;

            diagnostic.Findings.Add(
                $"The dominant resistance layer is " +
                $"{dominantLayer.LayerName}, contributing " +
                $"{dominantLayer.ResistanceContributionPercentage:F1}% " +
                "of total assembly resistance.");

            // Check whether the dominant layer has verified data.

            if (dominantLayer.DataConfidence != "VERIFIED")
            {
                diagnostic.Findings.Add(
                    "The dominant resistance layer does not use " +
                    "verified manufacturer conductivity data. " +
                    "Its material properties should be reviewed.");
            }

            // Identify layers with unverified data.

            int unverifiedCount = diagnostic.Layers.Count(
                layer => layer.DataConfidence == "UNVERIFIED");

            if (unverifiedCount > 0)
            {
                diagnostic.Findings.Add(
                    $"{unverifiedCount} layer(s) have unknown or " +
                    "unverified conductivity provenance.");
            }

            // Identify Revit thermal asset usage.

            int revitAssetCount = diagnostic.Layers.Count(
                layer => layer.DataConfidence == "REVIT DATA");

            if (revitAssetCount > 0)
            {
                diagnostic.Findings.Add(
                    $"{revitAssetCount} layer(s) use Revit thermal asset data.");
            }
        }


        // ============================================
        // ASSEMBLY SUMMARY
        // ============================================

        private void GenerateSummary(
            LayerDiagnosticResult diagnostic)
        {
            if (diagnostic.DominantLayer == null)
            {
                diagnostic.Summary =
                    "No dominant thermal resistance layer could be identified.";

                return;
            }

            var dominantLayer = diagnostic.DominantLayer;

            diagnostic.Summary =
                $"{dominantLayer.LayerName} is the dominant " +
                $"thermal resistance layer, contributing " +
                $"{dominantLayer.ResistanceContributionPercentage:F1}% " +
                $"of the assembly's total resistance. " +
                $"Calculated U-value: " +
                $"{diagnostic.UValue:F3} W/(m²·K).";
        }
    }


    // ============================================
    // ASSEMBLY DIAGNOSTIC RESULT
    // ============================================

    public class LayerDiagnosticResult
    {
        public string AssemblyName { get; set; } = "";

        public bool IsCalculationValid { get; set; }

        public double? UValue { get; set; }

        public double? TotalResistance { get; set; }

        public string Status { get; set; } = "NOT ANALYSED";

        public string Summary { get; set; } = "";

        public LayerDiagnosticItem? DominantLayer { get; set; }

        public List<LayerDiagnosticItem> Layers { get; set; } = new();

        public List<string> Findings { get; set; } = new();
    }


    // ============================================
    // INDIVIDUAL LAYER DIAGNOSTIC RESULT
    // ============================================

    public class LayerDiagnosticItem
    {
        public int LayerNumber { get; set; }

        public string LayerName { get; set; } = "";

        public double ThicknessMm { get; set; }

        public double? ThermalConductivityWmK { get; set; }

        public double ThermalResistance { get; set; }

        public double ResistanceContributionPercentage { get; set; }

        public string ConductivitySource { get; set; } = "";

        public string ConductivitySourceDescription { get; set; } = "";

        public string DataConfidence { get; set; } = "";

        public string ContributionCategory { get; set; } = "";

        public string Explanation { get; set; } = "";

        public string ConductivityExplanation { get; set; } = "";

        public List<string> Findings { get; set; } = new();
    }
}