using System;
using System.Collections.Generic;
using System.Linq;

using SpecSynthAI.Services;

using ThermalEngine.Models;
using ThermalEngine.Services;
using ThermalEngine.Configuration;

namespace SpecSynthAI.Diagnostics
{
    /// <summary>
    /// Dependency-free validation runner for SpecSynth AI.
    /// Tests diagnostic services and thermal calculation behaviour.
    /// Does not access or modify the active Revit model.
    ///
    /// Run:
    /// DiagnosticValidationRunner.RunAll()
    /// from Visual Studio Immediate Window while Revit is running.
    /// </summary>
    public static class DiagnosticValidationRunner
    {
        public static List<DiagnosticValidationCaseResult> RunAll()
        {
            var results = new List<DiagnosticValidationCaseResult>();

            // =====================================================
            // EXISTING DIAGNOSTIC TESTS
            // =====================================================

            RunCase(
                results,
                "T1 - Verified manufacturer conductivity",
                CreateAssembly(
                    "T1 Verified Assembly",
                    new ThermalLayer
                    {
                        LayerName = "PIR insulation",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = 0.020,
                        ConductivitySource = ConductivitySource.VerifiedManufacturerProduct
                    }),
                true, "COMPLETE", 0, 0, 0, 0);

            RunCase(
                results,
                "T2 - Revit thermal asset conductivity",
                CreateAssembly(
                    "T2 Revit Asset Assembly",
                    new ThermalLayer
                    {
                        LayerName = "Concrete block",
                        ThicknessMm = 200,
                        ThermalConductivityWmK = 1.046,
                        ConductivitySource = ConductivitySource.RevitThermalAsset
                    }),
                true, "REQUIRES REVIEW", 0, 0, 0, 1);

            RunCase(
                results,
                "T3 - Missing conductivity",
                CreateAssembly(
                    "T3 Missing Conductivity Assembly",
                    new ThermalLayer
                    {
                        LayerName = "Unknown insulation",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = null,
                        ConductivitySource = ConductivitySource.Unknown
                    }),
                false, "REQUIRES REVIEW", 1, 0, 0, 1);

            RunCase(
                results,
                "T4 - Invalid conductivity",
                CreateAssembly(
                    "T4 Invalid Conductivity Assembly",
                    new ThermalLayer
                    {
                        LayerName = "Invalid material",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = 0,
                        ConductivitySource = ConductivitySource.RevitThermalAsset
                    }),
                false, "INVALID DATA", 0, 1, 0, 1);

            RunCase(
                results,
                "T5 - Invalid thickness",
                CreateAssembly(
                    "T5 Invalid Thickness Assembly",
                    new ThermalLayer
                    {
                        LayerName = "PIR insulation",
                        ThicknessMm = 0,
                        ThermalConductivityWmK = 0.020,
                        ConductivitySource = ConductivitySource.VerifiedManufacturerProduct
                    }),
                false, "INVALID DATA", 0, 0, 1, 1);

            RunCase(
                results,
                "T6 - Independent data-quality assessment",
                CreateAssembly(
                    "T6 Invalid Calculation Assembly",
                    new ThermalLayer
                    {
                        LayerName = "Missing conductivity layer",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = null,
                        ConductivitySource = ConductivitySource.Unknown
                    },
                    new ThermalLayer
                    {
                        LayerName = "Revit asset layer",
                        ThicknessMm = 150,
                        ThermalConductivityWmK = 0.70,
                        ConductivitySource = ConductivitySource.RevitThermalAsset
                    }),
                false, "REQUIRES REVIEW", 1, 0, 0, 2);


            // =====================================================
            // NEW THERMAL CALCULATION REGRESSION TESTS
            // =====================================================

            RunSimpleTest(
                results,
                "T7 - Exact layer resistance",
                () =>
                {
                    var calculator = new ThermalCalculator();

                    var layer = new ThermalLayer
                    {
                        LayerName = "Test insulation",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = 0.040
                    };

                    double actual =
                        calculator.CalculateLayerResistance(layer);

                    AssertNear(
                        actual,
                        2.5,
                        0.000001,
                        "Expected R = 2.5 m²K/W.");
                });


            RunSimpleTest(
                results,
                "T8 - Assembly U-value",
                () =>
                {
                    var calculator = new ThermalCalculator();

                    var layers = new List<ThermalLayer>
                    {
                        new ThermalLayer
                        {
                            LayerName = "Test insulation",
                            ThicknessMm = 100,
                            ThermalConductivityWmK = 0.040
                        }
                    };

                    SurfaceResistanceValues surface =
                        SurfaceResistanceDefaults.Get(
                            HeatFlowDirection.Horizontal);

                    double expected =
                        1.0 /
                        (surface.InternalResistance +
                         2.5 +
                         surface.ExternalResistance);

                    double actual =
                        calculator.CalculateUValue(
                            layers,
                            HeatFlowDirection.Horizontal);

                    AssertNear(
                        actual,
                        expected,
                        0.000001,
                        "Assembly U-value does not match expected result.");
                });


            RunSimpleTest(
                results,
                "T9 - Manufacturer-declared and calculated R separation",
                () =>
                {
                    var calculator = new ThermalCalculator();

                    var layer = new ThermalLayer
                    {
                        LayerName = "Manufacturer declared insulation",
                        ThicknessMm = 100,
                        ThermalConductivityWmK = 0.040,

                        ProductId = "TEST_PRODUCT",
                        VariantId = "TEST_100MM",

                        ConductivitySource =
                            ConductivitySource.VerifiedManufacturerProduct,

                        ResistanceScope =
                            ResistanceValueScope.ProductVariant,

                        ManufacturerDeclaredResistance = 2.4,

                        ResistanceStatus = "manufacturer_declared"
                    };

                    ThermalCalculationResult calculation =
                        calculator.CalculateAssembly(
                            new[] { layer },
                            HeatFlowDirection.Horizontal);

                    if (!calculation.IsValid)
                        throw new Exception("Calculation was invalid.");

                    ThermalLayerResult result =
                        calculation.LayerResults.Single();

                    AssertNear(
                        result.ThermalResistance,
                        2.5,
                        0.000001,
                        "Calculated R-value is incorrect.");

                    AssertNear(
                        result.ManufacturerDeclaredResistance!.Value,
                        2.4,
                        0.000001,
                        "Manufacturer-declared R-value was not preserved.");

                    AssertNear(
                        result.SelectedResistance!.Value,
                        2.5,
                        0.000001,
                        "Selected resistance should remain the calculated value.");

                    AssertNear(
                        result.DiscrepancyPercentage!.Value,
                        4.1666666667,
                        0.0001,
                        "Resistance discrepancy is incorrect.");

                    if (result.ResistanceSelectionMethod !=
                        "CalculatedFromThicknessAndConductivity")
                    {
                        throw new Exception(
                            "Resistance calculation method was not preserved.");
                    }
                });


            RunSimpleTest(
                results,
                "T10 - Equivalent wall resistance scope protection",
                () =>
                {
                    var calculator = new ThermalCalculator();

                    var layer = new ThermalLayer
                    {
                        LayerName = "Equivalent wall layer",
                        ThicknessMm = 240,
                        ThermalConductivityWmK = 0.221,

                        ResistanceScope =
                            ResistanceValueScope.EquivalentWallOrAssembly,

                        ManufacturerDeclaredResistance = 1.07,

                        ResistanceStatus =
                            "manufacturer_declared_wall_value"
                    };

                    ThermalCalculationResult calculation =
                        calculator.CalculateAssembly(
                            new[] { layer },
                            HeatFlowDirection.Horizontal);

                    if (!calculation.IsValid)
                        throw new Exception("Calculation was invalid.");

                    ThermalLayerResult result =
                        calculation.LayerResults.Single();

                    if (result.DiscrepancyPercentage.HasValue)
                    {
                        throw new Exception(
                            "Equivalent wall resistance was incorrectly compared " +
                            "against an individual layer calculation.");
                    }

                    if (result.ManufacturerDeclaredResistance != 1.07)
                    {
                        throw new Exception(
                            "Manufacturer-declared wall resistance was not preserved.");
                    }
                });


            return results;
        }


        // =========================================================
        // EXISTING DIAGNOSTIC TEST EXECUTION
        // =========================================================

        private static WallAssembly CreateAssembly(
            string name,
            params ThermalLayer[] layers)
        {
            return new WallAssembly
            {
                AssemblyName = name,
                Layers = layers.ToList()
            };
        }


        private static void RunCase(
            List<DiagnosticValidationCaseResult> results,
            string testName,
            WallAssembly assembly,
            bool expectedCalculationValid,
            string expectedDataStatus,
            int expectedMissingConductivity,
            int expectedInvalidConductivity,
            int expectedInvalidThickness,
            int expectedReviewLayers)
        {
            var caseResult = new DiagnosticValidationCaseResult
            {
                TestName = testName
            };

            try
            {
                var calculator = new ThermalCalculator();

                ThermalCalculationResult calculation =
                    calculator.CalculateAssembly(
                        assembly.Layers,
                        HeatFlowDirection.Horizontal);

                var reportService =
                    new AssemblyDiagnosticReportService();

                AssemblyDiagnosticReport report =
                    reportService.Generate(assembly, calculation);

                caseResult.ActualCalculationStatus =
                    report.CalculationStatus;

                caseResult.ActualDataStatus =
                    report.InputDataStatus;

                caseResult.ActualMissingConductivity =
                    report.MaterialDataQuality.MissingConductivityLayers;

                caseResult.ActualInvalidConductivity =
                    report.MaterialDataQuality.InvalidConductivityLayers;

                caseResult.ActualInvalidThickness =
                    report.MaterialDataQuality.InvalidThicknessLayers;

                caseResult.ActualLayersRequiringReview =
                    report.LayersRequiringReview;

                caseResult.UValue = report.UValue;

                var failures = new List<string>();

                if (report.CalculationIsValid != expectedCalculationValid)
                    failures.Add("Unexpected calculation validity.");

                if (!string.Equals(
                    report.InputDataStatus,
                    expectedDataStatus,
                    StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add(
                        $"Expected data status '{expectedDataStatus}', " +
                        $"got '{report.InputDataStatus}'.");
                }

                if (caseResult.ActualMissingConductivity != expectedMissingConductivity)
                    failures.Add("Missing-conductivity count did not match.");

                if (caseResult.ActualInvalidConductivity != expectedInvalidConductivity)
                    failures.Add("Invalid-conductivity count did not match.");

                if (caseResult.ActualInvalidThickness != expectedInvalidThickness)
                    failures.Add("Invalid-thickness count did not match.");

                if (caseResult.ActualLayersRequiringReview != expectedReviewLayers)
                    failures.Add("Layers-requiring-review count did not match.");

                if (testName.StartsWith("T6", StringComparison.Ordinal) &&
                    report.MaterialDataQuality.TotalLayers != assembly.Layers.Count)
                {
                    failures.Add(
                        "Data quality was not assessed independently of calculation validity.");
                }

                caseResult.Passed = failures.Count == 0;

                caseResult.Message = caseResult.Passed
                    ? "All expected outcomes matched."
                    : string.Join(" ", failures);
            }
            catch (Exception ex)
            {
                caseResult.Passed = false;
                caseResult.Message =
                    "Test threw an exception: " + ex.Message;
            }

            results.Add(caseResult);
        }


        // =========================================================
        // NEW SIMPLE TEST EXECUTION
        // =========================================================

        private static void RunSimpleTest(
            List<DiagnosticValidationCaseResult> results,
            string testName,
            Action test)
        {
            var caseResult = new DiagnosticValidationCaseResult
            {
                TestName = testName
            };

            try
            {
                test();

                caseResult.Passed = true;
                caseResult.ActualCalculationStatus = "VALID";
                caseResult.ActualDataStatus = "TEST PASSED";
                caseResult.Message = "All expected outcomes matched.";
            }
            catch (Exception ex)
            {
                caseResult.Passed = false;
                caseResult.ActualCalculationStatus = "TEST FAILED";
                caseResult.ActualDataStatus = "REQUIRES REVIEW";
                caseResult.Message = ex.Message;
            }

            results.Add(caseResult);
        }


        // =========================================================
        // NUMERICAL ASSERTION
        // =========================================================

        private static void AssertNear(
            double actual,
            double expected,
            double tolerance,
            string message)
        {
            if (!double.IsFinite(actual) ||
                Math.Abs(actual - expected) > tolerance)
            {
                throw new Exception(
                    $"{message} Expected: {expected}; Actual: {actual}.");
            }
        }
    }


    // =============================================================
    // TEST RESULT MODEL
    // =============================================================

    public class DiagnosticValidationCaseResult
    {
        public string TestName { get; set; } = "";

        public bool Passed { get; set; }

        public string ActualCalculationStatus { get; set; } = "";

        public string ActualDataStatus { get; set; } = "";

        public int ActualMissingConductivity { get; set; }

        public int ActualInvalidConductivity { get; set; }

        public int ActualInvalidThickness { get; set; }

        public int ActualLayersRequiringReview { get; set; }

        public double? UValue { get; set; }

        public string Message { get; set; } = "";
    }
}