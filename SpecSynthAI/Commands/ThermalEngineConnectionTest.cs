using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Autodesk.Revit.UI;

using ThermalEngine.Configuration;
using ThermalEngine.Models;
using ThermalEngine.Services;

namespace SpecSynthAI.Commands;

public class ThermalEngineConnectionTest
{
    public static void Test()
    {
        var calculator = new ThermalCalculator();

        // ==========================================
        // REFERENCE WALL ASSEMBLY
        // ==========================================

        var layers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Internal Plaster",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70,
                ConductivitySource =
                    ConductivitySource.VerifiedManufacturerProduct
            },

            new()
            {
                LayerName = "Brick",
                ThicknessMm = 100,
                ThermalConductivityWmK = 0.77,
                ConductivitySource =
                    ConductivitySource.VerifiedManufacturerProduct
            },

            new()
            {
                LayerName = "PIR Insulation",
                ThicknessMm = 100,
                ThermalConductivityWmK = 0.022,
                ConductivitySource =
                    ConductivitySource.VerifiedManufacturerProduct
            },

            new()
            {
                LayerName = "Plasterboard",
                ThicknessMm = 12.5,
                ThermalConductivityWmK = 0.25,
                ConductivitySource =
                    ConductivitySource.VerifiedManufacturerProduct
            }
        };

        // ==========================================
        // T01: REFERENCE WALL VALIDATION
        // ==========================================

        RunT01(calculator, layers);

        // ==========================================
        // T02: INVALID MATERIAL DATA
        // ==========================================

        RunT02(calculator);

        // ==========================================
        // T03: LAYER RESISTANCE AND CONTRIBUTIONS
        // ==========================================

        RunT03(calculator, layers);

        // ==========================================
        // T04: HEAT-FLOW DIRECTION
        // ==========================================

        RunT04(calculator, layers);

        // ==========================================
        // T05: ASSEMBLY VALIDATION
        // ==========================================

        RunT05(calculator);
    }


    // ==========================================
    // T01: REFERENCE WALL CALCULATION
    // ==========================================

    private static void RunT01(
        ThermalCalculator calculator,
        List<ThermalLayer> layers)
    {
        const double tolerance = 0.000001;

        var result = calculator.CalculateAssembly(
            layers,
            HeatFlowDirection.Horizontal);

        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "T01 FAILED: " +
                string.Join("; ", result.Errors));
        }

        double expectedLayerResistance = 4.746753246753247;
        double expectedTotalResistance = 4.916753246753247;
        double expectedUValue = 0.2033862489764653;

        bool passed =
            result.LayerResistance.HasValue &&
            result.TotalResistance.HasValue &&
            result.UValue.HasValue &&

            Math.Abs(
                result.LayerResistance.Value -
                expectedLayerResistance) < tolerance &&

            Math.Abs(
                result.TotalResistance.Value -
                expectedTotalResistance) < tolerance &&

            Math.Abs(
                result.UValue.Value -
                expectedUValue) < tolerance;

        var report = new StringBuilder();

        report.AppendLine("T01 - REFERENCE WALL CALCULATION");
        report.AppendLine();

        report.AppendLine(
            $"Layer Resistance: {result.LayerResistance:F6}");

        report.AppendLine(
            $"Total Resistance: {result.TotalResistance:F6}");

        report.AppendLine(
            $"U-value: {result.UValue:F6} W/(m²·K)");

        report.AppendLine();

        report.AppendLine(
            $"T01 STATUS: {(passed ? "PASS" : "FAIL")}");

        TaskDialog.Show(
            "SpecSynth AI - T01 Validation",
            report.ToString());

        if (!passed)
        {
            throw new InvalidOperationException(
                "T01 FAILED: Reference calculation mismatch.");
        }
    }


    // ==========================================
    // T02: INVALID MATERIAL DATA
    // ==========================================

    private static void RunT02(
        ThermalCalculator calculator)
    {
        var report = new StringBuilder();

        int passed = 0;
        const int total = 4;

        report.AppendLine("T02 - INVALID MATERIAL DATA");
        report.AppendLine("------------------------------");

        // T02.1: Missing conductivity

        var missingConductivity = new ThermalLayer
        {
            LayerName = "Unknown Insulation",
            ThicknessMm = 100,
            ThermalConductivityWmK = null
        };

        if (ValidateRejectedAssembly(
            calculator,
            missingConductivity,
            "T02.1 - Missing Conductivity",
            report))
        {
            passed++;
        }

        // T02.2: Zero conductivity

        var zeroConductivity = new ThermalLayer
        {
            LayerName = "Invalid Insulation",
            ThicknessMm = 100,
            ThermalConductivityWmK = 0
        };

        if (ValidateRejectedAssembly(
            calculator,
            zeroConductivity,
            "T02.2 - Zero Conductivity",
            report))
        {
            passed++;
        }

        // T02.3: Negative conductivity

        var negativeConductivity = new ThermalLayer
        {
            LayerName = "Invalid Material",
            ThicknessMm = 100,
            ThermalConductivityWmK = -0.5
        };

        if (ValidateRejectedAssembly(
            calculator,
            negativeConductivity,
            "T02.3 - Negative Conductivity",
            report))
        {
            passed++;
        }

        // T02.4: Zero thickness

        var zeroThickness = new ThermalLayer
        {
            LayerName = "Invalid Layer",
            ThicknessMm = 0,
            ThermalConductivityWmK = 0.25
        };

        if (ValidateRejectedAssembly(
            calculator,
            zeroThickness,
            "T02.4 - Zero Thickness",
            report))
        {
            passed++;
        }

        bool t02Passed = passed == total;

        report.AppendLine();
        report.AppendLine("------------------------------");
        report.AppendLine($"Tests Passed: {passed}/{total}");
        report.AppendLine(
            $"T02 STATUS: {(t02Passed ? "PASS" : "FAIL")}");

        TaskDialog.Show(
            "SpecSynth AI - T02 Validation",
            report.ToString());

        if (!t02Passed)
        {
            throw new InvalidOperationException(
                $"T02 FAILED: {passed}/{total} tests passed.");
        }
    }


    // ==========================================
    // T02 HELPER: REJECT INVALID MATERIAL
    // ==========================================

    private static bool ValidateRejectedAssembly(
        ThermalCalculator calculator,
        ThermalLayer layer,
        string testName,
        StringBuilder report)
    {
        var layers = new List<ThermalLayer>
        {
            layer
        };

        var result = calculator.CalculateAssembly(
            layers,
            HeatFlowDirection.Horizontal);

        bool passed =
            !result.IsValid &&
            result.Errors.Count > 0;

        string conductivity =
            layer.ThermalConductivityWmK.HasValue
                ? layer.ThermalConductivityWmK.Value.ToString("F4")
                : "MISSING";

        report.AppendLine();
        report.AppendLine(testName);
        report.AppendLine($"Material: {layer.LayerName}");
        report.AppendLine($"Thickness: {layer.ThicknessMm} mm");
        report.AppendLine($"Conductivity: {conductivity}");

        report.AppendLine(
            $"Calculation Valid: {result.IsValid}");

        report.AppendLine(
            $"Validation Error: {string.Join("; ", result.Errors)}");

        report.AppendLine(
            $"Result: {(passed ? "PASS" : "FAIL")}");

        return passed;
    }


    // ==========================================
    // T03: LAYER RESISTANCE AND CONTRIBUTIONS
    // ==========================================

    private static void RunT03(
        ThermalCalculator calculator,
        List<ThermalLayer> layers)
    {
        const double tolerance = 0.000001;

        var result = calculator.CalculateAssembly(
            layers,
            HeatFlowDirection.Horizontal);

        if (!result.IsValid ||
            !result.LayerResistance.HasValue ||
            !result.TotalResistance.HasValue)
        {
            throw new InvalidOperationException(
                "T03 FAILED: Invalid calculation result.");
        }

        double[] expectedResistance =
        {
            0.0214285714285714,
            0.1298701298701299,
            4.545454545454545,
            0.05
        };

        if (result.LayerResults.Count != layers.Count)
        {
            throw new InvalidOperationException(
                "T03 FAILED: Layer result count mismatch.");
        }

        var report = new StringBuilder();

        report.AppendLine("T03 - LAYER RESISTANCE VALIDATION");
        report.AppendLine("--------------------------------");

        bool allLayersPassed = true;
        double contributionTotal = 0;

        for (int i = 0; i < layers.Count; i++)
        {
            var layer = result.LayerResults[i];

            double expectedContribution =
                expectedResistance[i] /
                result.TotalResistance.Value * 100.0;

            double actualContribution =
                layer.ResistanceContributionPercentage;

            bool resistancePassed =
                Math.Abs(
                    layer.ThermalResistance -
                    expectedResistance[i]) < tolerance;

            bool contributionPassed =
                Math.Abs(
                    actualContribution -
                    expectedContribution) < tolerance;

            bool layerPassed =
                resistancePassed && contributionPassed;

            if (!layerPassed)
            {
                allLayersPassed = false;
            }

            contributionTotal += actualContribution;

            report.AppendLine();
            report.AppendLine($"Layer: {layer.LayerName}");

            report.AppendLine(
                $"Expected R: {expectedResistance[i]:F6}");

            report.AppendLine(
                $"Actual R: {layer.ThermalResistance:F6}");

            report.AppendLine(
                $"Expected Contribution: {expectedContribution:F4}%");

            report.AppendLine(
                $"Actual Contribution: {actualContribution:F4}%");

            report.AppendLine(
                $"Result: {(layerPassed ? "PASS" : "FAIL")}");
        }

        double expectedContributionTotal =
            result.LayerResistance.Value /
            result.TotalResistance.Value * 100.0;

        bool totalPassed =
            Math.Abs(
                contributionTotal -
                expectedContributionTotal) < tolerance;

        bool t03Passed =
            allLayersPassed && totalPassed;

        report.AppendLine();
        report.AppendLine("--------------------------------");

        report.AppendLine(
            $"Total Layer Contribution: {contributionTotal:F4}%");

        report.AppendLine(
            $"Expected Layer Contribution: {expectedContributionTotal:F4}%");

        report.AppendLine(
            $"Contribution Check: {(totalPassed ? "PASS" : "FAIL")}");

        report.AppendLine();
        report.AppendLine(
            $"T03 STATUS: {(t03Passed ? "PASS" : "FAIL")}");

        TaskDialog.Show(
            "SpecSynth AI - T03 Validation",
            report.ToString());

        if (!t03Passed)
        {
            throw new InvalidOperationException(
                "T03 FAILED: Layer resistance or contribution mismatch.");
        }
    }


    // ==========================================
    // T04: HEAT-FLOW DIRECTION
    // ==========================================

    private static void RunT04(
        ThermalCalculator calculator,
        List<ThermalLayer> layers)
    {
        const double tolerance = 0.000001;

        double layerResistance = 4.746753246753247;

        var report = new StringBuilder();

        report.AppendLine("T04 - HEAT-FLOW DIRECTION VALIDATION");
        report.AppendLine("------------------------------------");

        bool allPassed = true;

        TestHeatFlowDirection(
            calculator,
            layers,
            HeatFlowDirection.Horizontal,
            "WALL - HORIZONTAL",
            0.13,
            0.04,
            layerResistance,
            tolerance,
            report,
            ref allPassed);

        TestHeatFlowDirection(
            calculator,
            layers,
            HeatFlowDirection.Upward,
            "ROOF - UPWARD",
            0.10,
            0.04,
            layerResistance,
            tolerance,
            report,
            ref allPassed);

        TestHeatFlowDirection(
            calculator,
            layers,
            HeatFlowDirection.Downward,
            "FLOOR - DOWNWARD",
            0.17,
            0.04,
            layerResistance,
            tolerance,
            report,
            ref allPassed);

        report.AppendLine();
        report.AppendLine("------------------------------------");

        report.AppendLine(
            $"T04 STATUS: {(allPassed ? "PASS" : "FAIL")}");

        TaskDialog.Show(
            "SpecSynth AI - T04 Validation",
            report.ToString());

        if (!allPassed)
        {
            throw new InvalidOperationException(
                "T04 FAILED: Heat-flow direction mismatch.");
        }
    }


    // ==========================================
    // T04 HELPER
    // ==========================================

    private static void TestHeatFlowDirection(
        ThermalCalculator calculator,
        List<ThermalLayer> layers,
        HeatFlowDirection direction,
        string directionName,
        double expectedRsi,
        double expectedRse,
        double expectedLayerResistance,
        double tolerance,
        StringBuilder report,
        ref bool allPassed)
    {
        var result = calculator.CalculateAssembly(
            layers,
            direction);

        if (!result.IsValid ||
            !result.InternalSurfaceResistance.HasValue ||
            !result.ExternalSurfaceResistance.HasValue ||
            !result.LayerResistance.HasValue ||
            !result.TotalResistance.HasValue ||
            !result.UValue.HasValue)
        {
            report.AppendLine();
            report.AppendLine($"{directionName}: FAIL");
            report.AppendLine("Invalid calculation result.");

            allPassed = false;
            return;
        }

        double expectedTotalResistance =
            expectedRsi +
            expectedLayerResistance +
            expectedRse;

        double expectedUValue =
            1.0 / expectedTotalResistance;

        bool passed =
            Math.Abs(
                result.InternalSurfaceResistance.Value -
                expectedRsi) < tolerance &&

            Math.Abs(
                result.ExternalSurfaceResistance.Value -
                expectedRse) < tolerance &&

            Math.Abs(
                result.LayerResistance.Value -
                expectedLayerResistance) < tolerance &&

            Math.Abs(
                result.TotalResistance.Value -
                expectedTotalResistance) < tolerance &&

            Math.Abs(
                result.UValue.Value -
                expectedUValue) < tolerance;

        if (!passed)
        {
            allPassed = false;
        }

        report.AppendLine();
        report.AppendLine(directionName);

        report.AppendLine($"Expected Rsi: {expectedRsi:F3}");
        report.AppendLine(
            $"Actual Rsi: {result.InternalSurfaceResistance.Value:F3}");

        report.AppendLine($"Expected Rse: {expectedRse:F3}");
        report.AppendLine(
            $"Actual Rse: {result.ExternalSurfaceResistance.Value:F3}");

        report.AppendLine(
            $"Expected Total R: {expectedTotalResistance:F6}");

        report.AppendLine(
            $"Actual Total R: {result.TotalResistance.Value:F6}");

        report.AppendLine(
            $"Expected U-value: {expectedUValue:F6}");

        report.AppendLine(
            $"Actual U-value: {result.UValue.Value:F6}");

        report.AppendLine(
            $"Result: {(passed ? "PASS" : "FAIL")}");
    }


    // ==========================================
    // T05: ASSEMBLY VALIDATION
    // ==========================================

    private static void RunT05(
        ThermalCalculator calculator)
    {
        const double tolerance = 0.000001;

        var report = new StringBuilder();

        report.AppendLine("T05 - ASSEMBLY VALIDATION");
        report.AppendLine("------------------------------");

        int passed = 0;
        const int total = 6;

        // ------------------------------------------
        // T05.1: Empty assembly
        // ------------------------------------------

        var emptyLayers = new List<ThermalLayer>();

        var emptyResult = calculator.CalculateAssembly(
            emptyLayers,
            HeatFlowDirection.Horizontal);

        bool emptyPassed =
            !emptyResult.IsValid &&
            emptyResult.Errors.Count > 0;

        AddT05Result(
            report,
            "T05.1 - Empty Assembly",
            emptyPassed,
            ref passed);

        // ------------------------------------------
        // T05.2: Null assembly
        // ------------------------------------------

        List<ThermalLayer>? nullLayers = null;

        var nullResult = calculator.CalculateAssembly(
            nullLayers!,
            HeatFlowDirection.Horizontal);

        bool nullPassed =
            !nullResult.IsValid &&
            nullResult.Errors.Count > 0;

        AddT05Result(
            report,
            "T05.2 - Null Assembly",
            nullPassed,
            ref passed);

        // ------------------------------------------
        // T05.3: Missing material name
        // ------------------------------------------

        var missingNameLayers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "",
                ThicknessMm = 100,
                ThermalConductivityWmK = 0.50
            }
        };

        var missingNameResult = calculator.CalculateAssembly(
            missingNameLayers,
            HeatFlowDirection.Horizontal);

        bool missingNamePassed =
            !missingNameResult.IsValid &&
            missingNameResult.Errors.Count > 0;

        AddT05Result(
            report,
            "T05.3 - Missing Material Name",
            missingNamePassed,
            ref passed);

        // ------------------------------------------
        // T05.4: NaN thickness
        // ------------------------------------------

        var nanLayers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Invalid Layer",
                ThicknessMm = double.NaN,
                ThermalConductivityWmK = 0.50
            }
        };

        var nanResult = calculator.CalculateAssembly(
            nanLayers,
            HeatFlowDirection.Horizontal);

        bool nanPassed =
            !nanResult.IsValid &&
            nanResult.Errors.Count > 0;

        AddT05Result(
            report,
            "T05.4 - NaN Thickness",
            nanPassed,
            ref passed);

        // ------------------------------------------
        // T05.5: Infinite conductivity
        // ------------------------------------------

        var infiniteLayers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Invalid Material",
                ThicknessMm = 100,
                ThermalConductivityWmK = double.PositiveInfinity
            }
        };

        var infiniteResult = calculator.CalculateAssembly(
            infiniteLayers,
            HeatFlowDirection.Horizontal);

        bool infinitePassed =
            !infiniteResult.IsValid &&
            infiniteResult.Errors.Count > 0;

        AddT05Result(
            report,
            "T05.5 - Infinite Conductivity",
            infinitePassed,
            ref passed);

        // ------------------------------------------
        // T05.6: Valid single-layer assembly
        // ------------------------------------------

        var validLayers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Test Material",
                ThicknessMm = 100,
                ThermalConductivityWmK = 0.50
            }
        };

        var validResult = calculator.CalculateAssembly(
            validLayers,
            HeatFlowDirection.Horizontal);

        double expectedLayerR = 0.20;
        double expectedTotalR = 0.37;
        double expectedUValue = 1.0 / 0.37;

        bool validPassed =
            validResult.IsValid &&
            validResult.LayerResistance.HasValue &&
            validResult.TotalResistance.HasValue &&
            validResult.UValue.HasValue &&

            Math.Abs(
                validResult.LayerResistance.Value -
                expectedLayerR) < tolerance &&

            Math.Abs(
                validResult.TotalResistance.Value -
                expectedTotalR) < tolerance &&

            Math.Abs(
                validResult.UValue.Value -
                expectedUValue) < tolerance;

        report.AppendLine();
        report.AppendLine("T05.6 - Valid Single-Layer Assembly");

        report.AppendLine(
            $"Expected Layer R: {expectedLayerR:F6}");

        report.AppendLine(
            $"Actual Layer R: {validResult.LayerResistance}");

        report.AppendLine(
            $"Expected Total R: {expectedTotalR:F6}");

        report.AppendLine(
            $"Actual Total R: {validResult.TotalResistance}");

        report.AppendLine(
            $"Expected U-value: {expectedUValue:F6}");

        report.AppendLine(
            $"Actual U-value: {validResult.UValue}");

        AddT05Result(
            report,
            "Valid Single-Layer Calculation",
            validPassed,
            ref passed);

        // ------------------------------------------
        // T05 SUMMARY
        // ------------------------------------------

        bool t05Passed = passed == total;

        report.AppendLine();
        report.AppendLine("------------------------------");

        report.AppendLine(
            $"Tests Passed: {passed}/{total}");

        report.AppendLine(
            $"T05 STATUS: {(t05Passed ? "PASS" : "FAIL")}");

        TaskDialog.Show(
            "SpecSynth AI - T05 Validation",
            report.ToString());

        if (!t05Passed)
        {
            throw new InvalidOperationException(
                $"T05 FAILED: {passed}/{total} tests passed.");
        }
    }


    // ==========================================
    // T05 HELPER: TEST REPORT
    // ==========================================

    private static void AddT05Result(
        StringBuilder report,
        string testName,
        bool testPassed,
        ref int passed)
    {
        if (testPassed)
        {
            passed++;
        }

        report.AppendLine();
        report.AppendLine(testName);

        report.AppendLine(
            $"Result: {(testPassed ? "PASS" : "FAIL")}");
    }
}