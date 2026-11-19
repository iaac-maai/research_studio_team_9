using System;
using System.Collections.Generic;
using System.Linq;

using ThermalEngine.Models;

namespace SpecSynthAI.Services
{
    /// <summary>
    /// Combines thermal layer diagnostics and material-input quality
    /// into one architect-facing report.
    ///
    /// Calculation validity and input-data confidence are reported
    /// separately. This service does not calculate or modify U-values.
    /// </summary>
    public class AssemblyDiagnosticReportService
    {
        private readonly LayerDiagnosticService _layerDiagnosticService;
        private readonly MaterialDataQualityService _materialDataQualityService;

        public AssemblyDiagnosticReportService()
        {
            _layerDiagnosticService = new LayerDiagnosticService();
            _materialDataQualityService = new MaterialDataQualityService();
        }

        public AssemblyDiagnosticReport Generate(
            WallAssembly assembly,
            ThermalCalculationResult calculation)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(calculation);

            LayerDiagnosticResult layerDiagnostics =
                _layerDiagnosticService.Diagnose(assembly, calculation);

            MaterialDataQualityResult dataQuality =
                _materialDataQualityService.Assess(assembly.Layers);

            var report = new AssemblyDiagnosticReport
            {
                AssemblyName = assembly.AssemblyName,

                // Keep these two assessments independent.
                CalculationIsValid = calculation.IsValid,
                CalculationStatus = calculation.IsValid ? "VALID" : "INVALID",

                InputDataStatus = dataQuality.Status,
                InputDataRequiresReview = dataQuality.LayersRequiringReview > 0,

                UValue = calculation.UValue,
                TotalResistance = calculation.TotalResistance,

                DominantLayerName =
                    layerDiagnostics.DominantLayer?.LayerName,

                DominantLayerContributionPercentage =
                    layerDiagnostics.DominantLayer?.ResistanceContributionPercentage,

                TotalLayerCount = dataQuality.TotalLayers,
                VerifiedManufacturerLayerCount = dataQuality.VerifiedLayers,
                RevitThermalAssetLayerCount = dataQuality.RevitAssetLayers,
                UnknownSourceLayerCount = dataQuality.UnknownSourceLayers,
                LayersRequiringReview = dataQuality.LayersRequiringReview,

                LayerDiagnostics = layerDiagnostics,
                MaterialDataQuality = dataQuality
            };

            BuildArchitectSummary(report);
            BuildObservations(report);
            BuildVerificationActions(report);

            return report;
        }

        private static void BuildArchitectSummary(AssemblyDiagnosticReport report)
        {
            if (!report.CalculationIsValid)
            {
                report.ArchitectSummary =
                    "Thermal performance could not be established because " +
                    "the assembly calculation is invalid. Review the calculation " +
                    "errors before using a U-value for design decisions.";
            }
            else
            {
                string dominantText =
                    !string.IsNullOrWhiteSpace(report.DominantLayerName)
                        ? $"{report.DominantLayerName} contributes " +
                          $"{report.DominantLayerContributionPercentage.GetValueOrDefault():F1}% " +
                          "of the assembly's layer resistance."
                        : "A dominant resistance layer could not be identified.";

                report.ArchitectSummary =
                    $"The assembly has a calculated U-value of " +
                    $"{report.UValue.GetValueOrDefault():F3} W/(m²·K). " +
                    $"{dominantText}";
            }

            if (report.InputDataRequiresReview)
            {
                report.ArchitectSummary +=
                    $" Material input review is also required for " +
                    $"{report.LayersRequiringReview} of {report.TotalLayerCount} layers. " +
                    "This is a data-provenance or completeness flag; it does not " +
                    "by itself mean the recorded material properties are incorrect.";
            }
            else if (report.CalculationIsValid)
            {
                report.ArchitectSummary +=
                    " The assessed material inputs are complete and use " +
                    "verified manufacturer conductivity provenance.";
            }
        }

        private static void BuildObservations(AssemblyDiagnosticReport report)
        {
            if (report.CalculationIsValid &&
                !string.IsNullOrWhiteSpace(report.DominantLayerName))
            {
                report.Observations.Add(
                    $"{report.DominantLayerName} is the dominant layer for " +
                    "steady-state thermal resistance.");
            }

            if (report.VerifiedManufacturerLayerCount > 0)
            {
                report.Observations.Add(
                    $"{report.VerifiedManufacturerLayerCount} layer(s) use " +
                    "verified manufacturer conductivity provenance.");
            }

            if (report.RevitThermalAssetLayerCount > 0)
            {
                report.Observations.Add(
                    $"{report.RevitThermalAssetLayerCount} layer(s) use " +
                    "Revit thermal asset values. These values are available " +
                    "for calculation but are not manufacturer-verified by this service.");
            }

            if (report.UnknownSourceLayerCount > 0)
            {
                report.Observations.Add(
                    $"{report.UnknownSourceLayerCount} layer(s) have unknown " +
                    "conductivity provenance.");
            }

            if (!report.CalculationIsValid)
            {
                foreach (string error in report.LayerDiagnostics.Findings)
                {
                    report.Observations.Add(error);
                }
            }

            if (report.Observations.Count == 0)
            {
                report.Observations.Add(
                    "No additional diagnostic observations were generated.");
            }
        }

        private static void BuildVerificationActions(AssemblyDiagnosticReport report)
        {
            MaterialDataQualityResult quality = report.MaterialDataQuality;

            if (quality.MissingConductivityLayers > 0)
            {
                report.VerificationActions.Add(
                    "Locate a reliable conductivity value for each layer with missing data.");
            }

            if (quality.InvalidConductivityLayers > 0)
            {
                report.VerificationActions.Add(
                    "Check the units and source of each invalid conductivity value.");
            }

            if (quality.InvalidThicknessLayers > 0)
            {
                report.VerificationActions.Add(
                    "Verify the thickness and unit conversion for each affected layer.");
            }

            if (quality.MissingNameLayers > 0)
            {
                report.VerificationActions.Add(
                    "Identify the material for each unnamed layer so its properties can be traced.");
            }

            if (quality.RevitAssetLayers > 0)
            {
                report.VerificationActions.Add(
                    "Where higher confidence is needed, compare Revit thermal asset values " +
                    "with the relevant manufacturer technical datasheet.");
            }

            if (quality.UnknownSourceLayers > 0)
            {
                report.VerificationActions.Add(
                    "Trace the source of conductivity values marked as unknown.");
            }

            if (!report.CalculationIsValid)
            {
                report.VerificationActions.Insert(
                    0,
                    "Resolve the thermal calculation errors before relying on the assembly U-value.");
            }

            if (report.VerificationActions.Count == 0)
            {
                report.VerificationActions.Add(
                    "No input-data verification action was identified by the current checks.");
            }
        }
    }

    /// <summary>
    /// Architect-facing assembly diagnostic report.
    /// Calculation validity is deliberately separate from input-data status.
    /// </summary>
    public class AssemblyDiagnosticReport
    {
        public string AssemblyName { get; set; } = "";

        public bool CalculationIsValid { get; set; }

        public string CalculationStatus { get; set; } = "NOT ANALYSED";

        public string InputDataStatus { get; set; } = "NOT ANALYSED";

        public bool InputDataRequiresReview { get; set; }

        public double? UValue { get; set; }

        public double? TotalResistance { get; set; }

        public string? DominantLayerName { get; set; }

        public double? DominantLayerContributionPercentage { get; set; }

        public int TotalLayerCount { get; set; }

        public int VerifiedManufacturerLayerCount { get; set; }

        public int RevitThermalAssetLayerCount { get; set; }

        public int UnknownSourceLayerCount { get; set; }

        public int LayersRequiringReview { get; set; }

        public string ArchitectSummary { get; set; } = "";

        public List<string> Observations { get; set; } = new();

        public List<string> VerificationActions { get; set; } = new();

        public LayerDiagnosticResult LayerDiagnostics { get; set; } = new();

        public MaterialDataQualityResult MaterialDataQuality { get; set; } = new();
    }
}
