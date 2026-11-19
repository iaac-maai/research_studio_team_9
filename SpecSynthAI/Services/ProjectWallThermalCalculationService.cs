using System;
using System.Collections.Generic;
using System.Linq;

using ThermalEngine.Models;

namespace SpecSynthAI.Services
{
    public class ProjectWallThermalReportService
    {
        private readonly ProjectWallThermalCalculationService
            _calculationService;

        public ProjectWallThermalReportService()
        {
            _calculationService =
                new ProjectWallThermalCalculationService();
        }


        // ==========================================
        // BUILD PROJECT THERMAL REPORT
        // ==========================================

        public WallComparisonReport BuildReport(
            ProjectWallCollectionResult project)
        {
            ArgumentNullException.ThrowIfNull(project);

            var results = new List<WallComparisonResult>();


            // ==========================================
            // STEP 1: CALCULATE EACH EXTRACTED WALL TYPE
            // ==========================================

            foreach (ProjectWallTypeRecord wall in project.WallTypes)
            {
                WallComparisonResult thermalResult;

                if (!wall.ExtractionSuccessful ||
                    wall.Assembly == null)
                {
                    thermalResult = new WallComparisonResult
                    {
                        AssemblyName = wall.WallTypeName,

                        IsValid = false,

                        Errors = new List<string>
                        {
                            string.IsNullOrWhiteSpace(wall.Message)
                                ? "Wall assembly extraction failed."
                                : wall.Message
                        }
                    };
                }
                else
                {
                    try
                    {
                        thermalResult =
                            _calculationService.Calculate(
                                wall.Assembly);
                    }
                    catch (Exception ex)
                    {
                        thermalResult = new WallComparisonResult
                        {
                            AssemblyName = wall.Assembly.AssemblyName,

                            IsValid = false,

                            Errors = new List<string>
                            {
                                ex.Message
                            }
                        };
                    }
                }

                // Store the result against its original wall record.

                wall.ThermalPerformance = thermalResult;

                results.Add(thermalResult);
            }


            // ==========================================
            // STEP 2: IDENTIFY VALID RESULTS
            // ==========================================

            List<WallComparisonResult> validResults = results
                .Where(result =>
                    result.IsValid &&
                    result.UValue.HasValue &&
                    double.IsFinite(result.UValue.Value) &&
                    result.UValue.Value > 0)
                .ToList();


            // ==========================================
            // STEP 3: RANK WALL ASSEMBLIES
            // ==========================================

            List<WallComparisonResult> rankedResults = results
                .OrderBy(result => result.IsValid ? 0 : 1)
                .ThenBy(result => result.UValue ?? double.MaxValue)
                .ToList();


            // ==========================================
            // STEP 4: IDENTIFY BEST-PERFORMING ASSEMBLY
            // ==========================================

            WallComparisonResult? bestAssembly = validResults
                .OrderBy(result => result.UValue)
                .FirstOrDefault();


            string? bestAssemblyName =
                bestAssembly?.AssemblyName;

            double? lowestUValue =
                bestAssembly?.UValue;


            // ==========================================
            // STEP 5: ESTABLISH AUTOMATIC BASELINE
            // ==========================================

            // Use the valid assembly with the highest U-value
            // as the automatic project baseline.
            //
            // A higher U-value indicates greater heat transfer.
            // Other assemblies are compared against this baseline.

            WallComparisonResult? baselineAssembly =
                validResults
                    .OrderByDescending(result => result.UValue)
                    .FirstOrDefault();


            string? baselineName =
                baselineAssembly?.AssemblyName;

            double? baselineUValue =
                baselineAssembly?.UValue;


            // ==========================================
            // STEP 6: CALCULATE IMPROVEMENT PERCENTAGES
            // ==========================================

            if (baselineUValue.HasValue &&
                baselineUValue.Value > 0)
            {
                foreach (WallComparisonResult result in validResults)
                {
                    if (!result.UValue.HasValue)
                        continue;

                    double improvement =
                        ((baselineUValue.Value - result.UValue.Value)
                        / baselineUValue.Value) * 100;

                    result.ImprovementPercentage = improvement;
                }
            }


            // ==========================================
            // STEP 7: CREATE PROJECT COMPARISON REPORT
            // ==========================================

            var report = new WallComparisonReport
            {
                Results = rankedResults,

                BestPerformingAssembly = bestAssemblyName,

                LowestUValue = lowestUValue,

                BaselineAssembly = baselineName,

                BaselineUValue = baselineUValue
            };

            return report;
        }
    }
}