using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SpecSynthAI.Services;

using ThermalEngine.Models;
using ThermalEngine.UI.Models;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ProjectThermalClassificationValidationCommand
        : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                var testCases = new List<TestCase>
                {
                    new("E01", "Exterior", 0.080, "GOOD"),
                    new("E02", "Exterior", 0.100, "GOOD"),
                    new("E03", "Exterior", 0.105, "MODERATE"),
                    new("E04", "Exterior", 0.110, "MODERATE"),
                    new("E05", "Exterior", 0.120, "POOR"),
                    new("E06", "Exterior", null, "NOT ANALYSED"),

                    new("I01", "Interior", 0.500, "GOOD"),
                    new("I02", "Interior", 0.630, "MODERATE"),
                    new("I03", "Interior", 0.700, "POOR"),

                    new("U01", "Foundation", 0.200, "UNSUPPORTED")
                };

                var records = testCases.Select(test =>
                    new ProjectWallRecord
                    {
                        WallTypeName = test.Id,
                        WallFunction = test.WallFunction,
                        InstanceCount = 1,

                        ThermalPerformance = test.UValue.HasValue
                            ? new WallComparisonResult
                            {
                                IsValid = true,
                                UValue = test.UValue
                            }
                            : new WallComparisonResult
                            {
                                IsValid = false,
                                UValue = null
                            }
                    }).ToList();

                var service =
                    new ProjectThermalClassificationService();

                ProjectThermalClassificationResult result =
                    service.ClassifyProject(records);

                var report = new StringBuilder();

                int passed = 0;

                report.AppendLine(
                    "PROJECT THERMAL CLASSIFICATION VALIDATION");

                report.AppendLine(
                    "----------------------------------------");

                for (int i = 0; i < testCases.Count; i++)
                {
                    string expected = testCases[i].Expected;

                    string actual =
                        records[i].ThermalClassificationStatus;

                    bool isPass = string.Equals(
                        expected,
                        actual,
                        StringComparison.OrdinalIgnoreCase);

                    if (isPass)
                        passed++;

                    report.AppendLine(
                        $"{testCases[i].Id} | " +
                        $"Expected: {expected} | " +
                        $"Actual: {actual} | " +
                        $"{(isPass ? "PASS" : "FAIL")}");
                }

                report.AppendLine(
                    "----------------------------------------");

                report.AppendLine(
                    $"RESULT: {passed}/{testCases.Count} PASS");

                report.AppendLine(
                    $"GOOD: {result.GoodCount}");

                report.AppendLine(
                    $"MODERATE: {result.ModerateCount}");

                report.AppendLine(
                    $"POOR: {result.PoorCount}");

                report.AppendLine(
                    $"NOT ANALYSED: {result.NotAnalysedCount}");

                report.AppendLine(
                    $"UNSUPPORTED: {result.UnsupportedCount}");

                TaskDialog.Show(
                    "SpecSynth AI - Classification Validation",
                    report.ToString());

                return passed == testCases.Count
                    ? Result.Succeeded
                    : Result.Failed;
            }
            catch (Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "Classification Validation Error",
                    ex.Message);

                return Result.Failed;
            }
        }

        private class TestCase
        {
            public string Id { get; }
            public string WallFunction { get; }
            public double? UValue { get; }
            public string Expected { get; }

            public TestCase(
                string id,
                string wallFunction,
                double? uValue,
                string expected)
            {
                Id = id;
                WallFunction = wallFunction;
                UValue = uValue;
                Expected = expected;
            }
        }
    }
}