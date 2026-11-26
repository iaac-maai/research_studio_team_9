using System;
using System.Collections.Generic;
using System.Linq;

using ThermalEngine.Models;

namespace ThermalEngine.Services;

public class WallComparisonService
{
    private readonly ThermalCalculator _thermalCalculator;

    public WallComparisonService()
    {
        _thermalCalculator = new ThermalCalculator();
    }

    public WallComparisonReport CompareAssemblies(
        IEnumerable<WallAssembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var comparisonResults = new List<WallComparisonResult>();


        // ==========================================
        // STEP 1: CALCULATE EVERY WALL ASSEMBLY
        // ==========================================

        foreach (var assembly in assemblies)
        {
            if (assembly == null)
            {
                comparisonResults.Add(new WallComparisonResult
                {
                    AssemblyName = "Unknown Assembly",

                    IsValid = false,

                    Errors = new List<string>
                    {
                        "Assembly is missing."
                    }
                });

                continue;
            }

            var calculation = _thermalCalculator.CalculateAssembly(
                assembly.Layers);

            // Transfer calculation results.
            comparisonResults.Add(new WallComparisonResult
            {
                AssemblyName = assembly.AssemblyName,

                IsValid = calculation.IsValid,

                LayerResistance = calculation.LayerResistance,

                InternalSurfaceResistance =
                    calculation.InternalSurfaceResistance,

                ExternalSurfaceResistance =
                    calculation.ExternalSurfaceResistance,

                TotalResistance = calculation.TotalResistance,

                UValue = calculation.UValue,

                LayerResults = calculation.LayerResults,

                Errors = calculation.Errors
            });
        }


        // ==========================================
        // STEP 2: IDENTIFY VALID RESULTS
        // ==========================================

        var validResults = comparisonResults
            .Where(result =>
                result.IsValid &&
                result.UValue.HasValue &&
                double.IsFinite(result.UValue.Value) &&
                result.UValue.Value > 0)
            .ToList();


        // ==========================================
        // STEP 3: ESTABLISH AUTOMATIC BASELINE
        // ==========================================

        // The assembly with the highest valid U-value
        // becomes the automatic comparison baseline.

        var baseline = validResults
            .OrderByDescending(result => result.UValue)
            .FirstOrDefault();


        // ==========================================
        // STEP 4: CALCULATE IMPROVEMENT PERCENTAGES
        // ==========================================

        if (baseline?.UValue is double baselineUValue &&
            baselineUValue > 0)
        {
            foreach (var result in validResults)
            {
                if (!result.UValue.HasValue)
                    continue;

                double improvement =
                    ((baselineUValue - result.UValue.Value)
                    / baselineUValue) * 100.0;

                result.ImprovementPercentage = improvement;
            }
        }


        // ==========================================
        // STEP 5: RANK WALL ASSEMBLIES
        // ==========================================

        var rankedResults = comparisonResults
            .OrderBy(result => result.IsValid ? 0 : 1)
            .ThenBy(result => result.UValue ?? double.MaxValue)
            .ToList();


        // ==========================================
        // STEP 6: IDENTIFY BEST-PERFORMING ASSEMBLY
        // ==========================================

        var bestAssembly = validResults
            .OrderBy(result => result.UValue)
            .FirstOrDefault();


        // ==========================================
        // STEP 7: GENERATE COMPARISON REPORT
        // ==========================================

        return new WallComparisonReport
        {
            Results = rankedResults,

            BestPerformingAssembly =
                bestAssembly?.AssemblyName,

            LowestUValue =
                bestAssembly?.UValue,

            BaselineAssembly =
                baseline?.AssemblyName,

            BaselineUValue =
                baseline?.UValue
        };
    }
}