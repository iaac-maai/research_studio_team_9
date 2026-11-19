using Autodesk.Revit.DB;
using SpecSynthAI.Models;
using SpecSynthAI.Requirements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThermalEngine.Configuration;
using ThermalEngine.Models;
using ThermalEngine.Services;

namespace SpecSynthAI.Services
{
    public static class ComplianceInspectionService
    {
        public static WallInspectionData InspectWall(
            Document document,
            ElementId elementId)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            Element element = document.GetElement(elementId);

            if (element is not Wall wall)
            {
                return new WallInspectionData
                {
                    ElementId = elementId.ToString(),
                    Message = "Selected element is not a wall."
                };
            }

            var inspection = new WallInspectionData
            {
                ElementId = wall.Id.ToString(),
                WallName = wall.Name,
                WallTypeName = wall.WallType.Name
            };

            // ==========================================
            // 1. WHOLE-WALL THERMAL ASSESSMENT
            // ==========================================

            ProductCatalog? catalog = null;

            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(ComplianceInspectionService).Assembly.Location) ?? "";

            string cataloguePath =
                Path.Combine(assemblyDirectory, "products.json");

            if (File.Exists(cataloguePath))
            {
                try
                {
                    catalog = ProductCatalog.Load(cataloguePath);

                    inspection.ProductsLoaded =
                        catalog.Products.Count;
                }
                catch
                {
                    catalog = null;
                }
            }

            AssessWholeWall(
                document,
                wall,
                catalog,
                inspection);

            // ==========================================
            // 2. PRODUCT REQUIREMENT
            // ==========================================

            ProjectRequirement conductivityRequirement =
                ProjectRequirementsStore.LoadThermalConductivity();

            inspection.MaximumThermalConductivity =
                conductivityRequirement.MaximumAllowedValue;

            inspection.ThermalUnit =
                conductivityRequirement.Unit;

            // ==========================================
            // 3. WALL STRUCTURE
            // ==========================================

            CompoundStructure? structure =
                wall.WallType.GetCompoundStructure();

            if (structure == null)
            {
                inspection.Message =
                    "The selected wall has no compound structure.";

                SetOverallStatus(inspection);

                return inspection;
            }

            // ==========================================
            // 4. PRODUCT VERIFICATION
            // ==========================================

            foreach (CompoundStructureLayer layer in structure.GetLayers())
            {
                if (layer.Function !=
                    MaterialFunctionAssignment.Insulation)
                {
                    continue;
                }

                Material? material =
                    document.GetElement(layer.MaterialId) as Material;

                string materialName =
                    material?.Name ?? "No material assigned";

                double thicknessMm =
                    UnitUtils.ConvertFromInternalUnits(
                        layer.Width,
                        UnitTypeId.Millimeters);

                Product? product =
                    catalog?.FindByRevitMaterialName(materialName);

                string thicknessMatch =
                    ProductCatalog.CheckThicknessMatch(
                        product,
                        thicknessMm);

                string thermalResult =
                    product == null
                        ? "CANNOT VERIFY"
                        : ComplianceChecker.CheckThermalConductivity(
                            product,
                            conductivityRequirement);

                ComplianceVisualResult visualResult =
                    ComplianceVisualStatusClassifier.Classify(
                        product,
                        thicknessMatch,
                        thermalResult);

                var layerData =
                    new InsulationLayerInspectionData
                    {
                        MaterialName = materialName,

                        InstalledThicknessMm = thicknessMm,

                        ProductMapping = product == null
                            ? "UNMAPPED"
                            : product.ProductId,

                        ThicknessMatch = thicknessMatch,

                        ThermalResult = thermalResult,

                        Manufacturer =
                            product?.Manufacturer ?? "Not available",

                        ProductName =
                            product?.ProductName ?? "Not available",

                        ProductId =
                            product?.ProductId ?? "Not available",

                        VerificationStatus =
                            product?.VerificationStatus ?? "UNMAPPED",

                        CatalogueThicknessMm =
                            product?.ThicknessMm,

                        ThermalConductivityWmK =
                            product?.ThermalConductivityWmK,

                        AvailableThicknesses =
                            product?.AvailableThicknessesMm == null ||
                            product.AvailableThicknessesMm.Count == 0
                                ? "Not documented"
                                : string.Join(
                                    ", ",
                                    product.AvailableThicknessesMm),

                        SourceDocument =
                            product?.SourceDocument ?? "",

                        SourceUrl =
                            product?.SourceUrl ?? "",

                        ComplianceStatus =
                            visualResult.Status,

                        ComplianceStatusLabel =
                            visualResult.Label,

                        ComplianceExplanation =
                            visualResult.Explanation
                    };

                inspection.InsulationLayers.Add(layerData);
            }

            inspection.Message =
                inspection.InsulationLayers.Count == 0
                    ? "Whole-wall thermal assessment completed. No insulation layers were identified for product verification."
                    : "Whole-wall thermal and product inspection completed.";

            // ==========================================
            // 5. COMBINE RESULTS
            // ==========================================

            SetOverallStatus(inspection);

            return inspection;
        }

        // ==========================================
        // WHOLE-WALL THERMAL ASSESSMENT
        // ==========================================

        private static void AssessWholeWall(
            Document document,
            Wall wall,
            ProductCatalog? catalog,
            WallInspectionData inspection)
        {
            try
            {
                var extractionService =
                    new RevitWallExtractionService();

                var calculator =
                    new ThermalCalculator();

                WallAssembly assembly =
                    extractionService.ExtractAssembly(
                        document,
                        wall,
                        catalog);

                ThermalCalculationResult calculation =
                    calculator.CalculateAssembly(
                        assembly.Layers,
                        HeatFlowDirection.Horizontal);

                if (!calculation.IsValid ||
                    !calculation.UValue.HasValue ||
                    !double.IsFinite(calculation.UValue.Value) ||
                    calculation.UValue.Value <= 0)
                {
                    string errors =
                        calculation.Errors.Count > 0
                            ? string.Join("; ", calculation.Errors)
                            : "The U-value calculation returned no valid result.";

                    SetWallThermalCannotVerify(
                        inspection,
                        errors);

                    return;
                }

                inspection.WallUValue =
                    calculation.UValue.Value;

                // Select project requirement by wall function.
                string wallFunction =
                    wall.WallType.Function.ToString();

                ProjectRequirement requirement;

                if (wallFunction.Equals(
                    "Exterior",
                    StringComparison.OrdinalIgnoreCase))
                {
                    requirement =
                        ProjectRequirementsStore.LoadExternalWallUValue();
                }
                else if (wallFunction.Equals(
                    "Interior",
                    StringComparison.OrdinalIgnoreCase))
                {
                    requirement =
                        ProjectRequirementsStore.LoadInternalWallUValue();
                }
                else
                {
                    SetWallThermalCannotVerify(
                        inspection,
                        $"Wall function '{wallFunction}' does not have a configured wall U-value requirement.");

                    return;
                }

                inspection.MaximumWallUValue =
                    requirement.MaximumAllowedValue;

                inspection.WallUValueRequirement =
                    requirement.Unit;

                double calculatedU =
                    calculation.UValue.Value;

                double maximumU =
                    requirement.MaximumAllowedValue;

                if (calculatedU <= maximumU)
                {
                    inspection.WallThermalStatus =
                        ComplianceVisualStatus.Pass;

                    inspection.WallThermalStatusLabel =
                        "PASS";

                    inspection.WallThermalExplanation =
                        $"Calculated U-value ({calculatedU:F3}) meets the project maximum ({maximumU:F3} {requirement.Unit}).";
                }
                else
                {
                    inspection.WallThermalStatus =
                        ComplianceVisualStatus.Fail;

                    inspection.WallThermalStatusLabel =
                        "FAIL";

                    inspection.WallThermalExplanation =
                        $"Calculated U-value ({calculatedU:F3}) exceeds the project maximum ({maximumU:F3} {requirement.Unit}).";
                }
            }
            catch (Exception ex)
            {
                SetWallThermalCannotVerify(
                    inspection,
                    $"Whole-wall thermal assessment failed: {ex.Message}");
            }
        }

        private static void SetWallThermalCannotVerify(
            WallInspectionData inspection,
            string explanation)
        {
            inspection.WallThermalStatus =
                ComplianceVisualStatus.Information;

            inspection.WallThermalStatusLabel =
                "CANNOT VERIFY";

            inspection.WallThermalExplanation =
                explanation;
        }

        // ==========================================
        // OVERALL STATUS
        // ==========================================

        private static void SetOverallStatus(
            WallInspectionData inspection)
        {
            var statuses =
                new List<ComplianceVisualStatus>();

            // Whole-wall thermal assessment is always applicable.
            statuses.Add(inspection.WallThermalStatus);

            // Product checks are applicable only when insulation
            // layers are present.
            statuses.AddRange(
                inspection.InsulationLayers.Select(
                    layer => layer.ComplianceStatus));

            ComplianceVisualStatus overall =
                statuses
                    .OrderByDescending(Priority)
                    .FirstOrDefault();

            inspection.ComplianceStatus = overall;

            inspection.ComplianceStatusLabel =
                overall switch
                {
                    ComplianceVisualStatus.Fail => "FAIL",
                    ComplianceVisualStatus.Warning => "WARNING",
                    ComplianceVisualStatus.Information => "CANNOT VERIFY",
                    ComplianceVisualStatus.Pass => "PASS",
                    _ => "CANNOT VERIFY"
                };

            inspection.ComplianceExplanation =
                BuildOverallExplanation(inspection);
        }

        private static string BuildOverallExplanation(
            WallInspectionData inspection)
        {
            if (inspection.ComplianceStatus ==
                ComplianceVisualStatus.Fail)
            {
                if (inspection.WallThermalStatus ==
                    ComplianceVisualStatus.Fail)
                {
                    return inspection.WallThermalExplanation;
                }

                InsulationLayerInspectionData? failedLayer =
                    inspection.InsulationLayers.FirstOrDefault(
                        layer => layer.ComplianceStatus ==
                            ComplianceVisualStatus.Fail);

                return failedLayer?.ComplianceExplanation ??
                    "One or more product requirements failed.";
            }

            if (inspection.ComplianceStatus ==
                ComplianceVisualStatus.Warning)
            {
                return "One or more product checks require attention.";
            }

            if (inspection.ComplianceStatus ==
                ComplianceVisualStatus.Information)
            {
                return inspection.WallThermalStatus ==
                    ComplianceVisualStatus.Information
                    ? inspection.WallThermalExplanation
                    : "One or more checks could not be verified.";
            }

            return "The whole-wall thermal requirement and all applicable product checks passed.";
        }

        private static int Priority(
            ComplianceVisualStatus status)
        {
            return status switch
            {
                ComplianceVisualStatus.Pass => 1,
                ComplianceVisualStatus.Information => 2,
                ComplianceVisualStatus.Warning => 3,
                ComplianceVisualStatus.Fail => 4,
                _ => 0
            };
        }
    }

    // ==============================================
    // WALL INSPECTION DATA
    // ==============================================

    public class WallInspectionData
    {
        public string ElementId { get; set; } = "";

        public string WallName { get; set; } = "";

        public string WallTypeName { get; set; } = "";

        public int ProductsLoaded { get; set; }

        public double MaximumThermalConductivity { get; set; }

        public string ThermalUnit { get; set; } = "";

        public string Message { get; set; } = "";

        public ComplianceVisualStatus ComplianceStatus { get; set; }
            = ComplianceVisualStatus.Information;

        public string ComplianceStatusLabel { get; set; }
            = "CANNOT VERIFY";

        public string ComplianceExplanation { get; set; } = "";

        // Whole-wall thermal assessment.
        public double? WallUValue { get; set; }

        public double? MaximumWallUValue { get; set; }

        public string WallUValueRequirement { get; set; } = "";

        public ComplianceVisualStatus WallThermalStatus { get; set; }
            = ComplianceVisualStatus.Information;

        public string WallThermalStatusLabel { get; set; }
            = "CANNOT VERIFY";

        public string WallThermalExplanation { get; set; } = "";

        public List<InsulationLayerInspectionData>
            InsulationLayers
        { get; set; } = new();
    }

    // ==============================================
    // INSULATION LAYER INSPECTION DATA
    // ==============================================

    public class InsulationLayerInspectionData
    {
        public string MaterialName { get; set; } = "";

        public double InstalledThicknessMm { get; set; }

        public string ProductMapping { get; set; } = "";

        public string ThicknessMatch { get; set; } = "";

        public string ThermalResult { get; set; } = "";

        public string Manufacturer { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string ProductId { get; set; } = "";

        public string VerificationStatus { get; set; } = "";

        public double? CatalogueThicknessMm { get; set; }

        public double? ThermalConductivityWmK { get; set; }

        public string AvailableThicknesses { get; set; } = "";

        public string SourceDocument { get; set; } = "";

        public string SourceUrl { get; set; } = "";

        public ComplianceVisualStatus ComplianceStatus { get; set; }
            = ComplianceVisualStatus.Information;

        public string ComplianceStatusLabel { get; set; }
            = "CANNOT VERIFY";

        public string ComplianceExplanation { get; set; } = "";
    }
}