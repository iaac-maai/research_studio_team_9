using System;
using System.Linq;

using ThermalEngine.Models;
using ThermalEngine.Services;

namespace SpecSynthAI.Services;

public class ProjectWallThermalCalculationService
{
    private readonly ThermalCalculator _thermalCalculator;

    public ProjectWallThermalCalculationService()
    {
        _thermalCalculator = new ThermalCalculator();
    }


    // ============================================
    // CALCULATE ONE WALL ASSEMBLY
    // ============================================

    public WallComparisonResult Calculate(WallAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var result = new WallComparisonResult
        {
            AssemblyName = assembly.AssemblyName,
            IsValid = false
        };


        // ========================================
        // 1. CHECK MATERIAL LAYERS
        // ========================================

        if (assembly.Layers == null ||
            !assembly.Layers.Any())
        {
            result.Errors.Add(
                "Wall assembly contains no material layers.");

            return result;
        }


        // ========================================
        // 2. RUN THERMAL ENGINE
        // ========================================

        ThermalCalculationResult calculationResult;

        try
        {
            calculationResult =
                _thermalCalculator.CalculateAssembly(
                    assembly.Layers);
        }
        catch (Exception ex)
        {
            result.Errors.Add(
                $"Thermal calculation failed: {ex.Message}");

            return result;
        }


        // ========================================
        // 3. HANDLE INVALID CALCULATION
        // ========================================

        if (!calculationResult.IsValid)
        {
            result.IsValid = false;

            result.Errors.AddRange(
                calculationResult.Errors);

            return result;
        }


        // ========================================
        // 4. MAP THERMAL RESULTS
        // ========================================

        result.IsValid = true;

        result.UValue = calculationResult.UValue;

        result.LayerResistance =
            calculationResult.LayerResistance;

        result.InternalSurfaceResistance =
            calculationResult.InternalSurfaceResistance;

        result.ExternalSurfaceResistance =
            calculationResult.ExternalSurfaceResistance;

        result.TotalResistance =
            calculationResult.TotalResistance;


        // ========================================
        // 5. MAP INDIVIDUAL LAYER RESULTS
        // ========================================

        result.LayerResults =
            calculationResult.LayerResults.ToList();


        return result;
    }
}