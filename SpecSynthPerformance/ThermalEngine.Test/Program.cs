using ThermalEngine.Configuration;
using ThermalEngine.Models;
using ThermalEngine.Services;

// ============================================
// 1. CREATE SERVICES
// ============================================

var comparisonService = new WallComparisonService();
var thermalCalculator = new ThermalCalculator();


// ============================================
// 2. DEFINE THREE WALL ASSEMBLIES
// ============================================

var assemblies = new List<WallAssembly>
{
    // WALL A: Conventional brick wall.
    new()
    {
        AssemblyName = "Wall A - Conventional Brick Wall",
        Layers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Internal Plaster",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            },
            new()
            {
                LayerName = "Brick",
                ThicknessMm = 200,
                ThermalConductivityWmK = 0.72
            },
            new()
            {
                LayerName = "External Render",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            }
        }
    },

    // WALL B: Brick wall with 100 mm mineral wool.
    new()
    {
        AssemblyName = "Wall B - Insulated Brick Wall",
        Layers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Internal Plaster",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            },
            new()
            {
                LayerName = "Brick",
                ThicknessMm = 200,
                ThermalConductivityWmK = 0.72
            },
            new()
            {
                LayerName = "Mineral Wool",
                ThicknessMm = 100,
                ThermalConductivityWmK = 0.040
            },
            new()
            {
                LayerName = "External Render",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            }
        }
    },

    // WALL C: Brick wall with 150 mm mineral wool.
    new()
    {
        AssemblyName = "Wall C - High Insulation Wall",
        Layers = new List<ThermalLayer>
        {
            new()
            {
                LayerName = "Internal Plaster",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            },
            new()
            {
                LayerName = "Brick",
                ThicknessMm = 200,
                ThermalConductivityWmK = 0.72
            },
            new()
            {
                LayerName = "Mineral Wool",
                ThicknessMm = 150,
                ThermalConductivityWmK = 0.040
            },
            new()
            {
                LayerName = "External Render",
                ThicknessMm = 15,
                ThermalConductivityWmK = 0.70
            }
        }
    }
};


// ============================================
// 3. RUN WALL COMPARISON
// ============================================

var report = comparisonService.CompareAssemblies(assemblies);

Console.WriteLine("========================================");
Console.WriteLine("SPECSYNTH - WALL ASSEMBLY COMPARISON");
Console.WriteLine("========================================");

int rank = 1;

foreach (var result in report.Results)
{
    Console.WriteLine($"\nRANK {rank}: {result.AssemblyName}");
    Console.WriteLine("----------------------------------------");

    if (result.IsValid)
    {
        Console.WriteLine(
            $"Total Resistance: {result.TotalResistance:F4} m²K/W");

        Console.WriteLine(
            $"U-Value: {result.UValue:F4} W/(m²K)");

        Console.WriteLine(
            $"Improvement: {result.ImprovementPercentage:F2}%");
    }
    else
    {
        Console.WriteLine("Calculation: INVALID");

        foreach (var error in result.Errors)
        {
            Console.WriteLine($"ERROR: {error}");
        }
    }

    rank++;
}


// ============================================
// 4. DISPLAY COMPARISON SUMMARY
// ============================================

Console.WriteLine("\n========================================");
Console.WriteLine("COMPARISON REPORT SUMMARY");
Console.WriteLine("========================================");

Console.WriteLine($"Baseline Assembly: {report.BaselineAssembly}");

Console.WriteLine(
    $"Baseline U-Value: {report.BaselineUValue:F4} W/(m²K)");

Console.WriteLine($"Best Assembly: {report.BestPerformingAssembly}");

Console.WriteLine(
    $"Lowest U-Value: {report.LowestUValue:F4} W/(m²K)");


// ============================================
// 5. TEST SURFACE RESISTANCE
// ============================================

Console.WriteLine("\n========================================");
Console.WriteLine("SURFACE RESISTANCE VALIDATION TEST");
Console.WriteLine("========================================");

// Revit wall data from our previous extraction test.
var revitTestWall = new WallAssembly
{
    AssemblyName = "SSAI_ExternalWall_RevitFallback_Test",

    Layers = new List<ThermalLayer>
    {
        new()
        {
            LayerName = "External Paint",
            ThicknessMm = 15,
            ThermalConductivityWmK = 0.0720
        },
        new()
        {
            LayerName = "Structural Blockwork",
            ThicknessMm = 200,
            ThermalConductivityWmK = 1.0460
        },
        new()
        {
            LayerName = "PIR Insulation",
            ThicknessMm = 100,
            ThermalConductivityWmK = 0.0200
        },
        new()
        {
            LayerName = "Internal Plasterboard",
            ThicknessMm = 12.5,
            ThermalConductivityWmK = 0.5200
        }
    }
};


// ============================================
// 6. CALCULATE WALL PERFORMANCE
// ============================================

var thermalResult = thermalCalculator.CalculateAssembly(
    revitTestWall.Layers,
    HeatFlowDirection.Horizontal
);

if (thermalResult.IsValid &&
    thermalResult.LayerResistance.HasValue &&
    thermalResult.InternalSurfaceResistance.HasValue &&
    thermalResult.ExternalSurfaceResistance.HasValue &&
    thermalResult.TotalResistance.HasValue &&
    thermalResult.UValue.HasValue)
{
    var surfaceValues = SurfaceResistanceDefaults.Get(
        HeatFlowDirection.Horizontal
    );

    // Expected values based on the test wall.
    double expectedLayerResistance = 5.4236;
    double expectedTotalResistance = 5.5936;
    double expectedUValue = 1.0 / expectedTotalResistance;

    Console.WriteLine($"\nAssembly: {revitTestWall.AssemblyName}");

    Console.WriteLine("\n--- RESISTANCE BREAKDOWN ---");

    Console.WriteLine(
        $"Material Layer Resistance: " +
        $"{thermalResult.LayerResistance.Value:F4} m²K/W");

    Console.WriteLine(
        $"Internal Surface Resistance (Rsi): " +
        $"{thermalResult.InternalSurfaceResistance.Value:F4} m²K/W");

    Console.WriteLine(
        $"External Surface Resistance (Rse): " +
        $"{thermalResult.ExternalSurfaceResistance.Value:F4} m²K/W");

    Console.WriteLine(
        $"Total Resistance: " +
        $"{thermalResult.TotalResistance.Value:F4} m²K/W");

    Console.WriteLine(
        $"U-Value: " +
        $"{thermalResult.UValue.Value:F4} W/(m²K)");


    // ========================================
    // 7. VALIDATION TESTS
    // ========================================

    Console.WriteLine("\n--- VALIDATION ---");

    bool layerResistancePassed =
        Math.Abs(
            thermalResult.LayerResistance.Value -
            expectedLayerResistance
        ) < 0.0001;

    bool internalSurfacePassed =
        Math.Abs(
            thermalResult.InternalSurfaceResistance.Value -
            surfaceValues.InternalResistance
        ) < 0.0001;

    bool externalSurfacePassed =
        Math.Abs(
            thermalResult.ExternalSurfaceResistance.Value -
            surfaceValues.ExternalResistance
        ) < 0.0001;

    bool resistancePassed =
        Math.Abs(
            thermalResult.TotalResistance.Value -
            expectedTotalResistance
        ) < 0.0001;

    bool uValuePassed =
        Math.Abs(
            thermalResult.UValue.Value -
            expectedUValue
        ) < 0.0001;


    Console.WriteLine(
        $"Layer Resistance Test: " +
        $"{(layerResistancePassed ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"Internal Surface Resistance Test: " +
        $"{(internalSurfacePassed ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"External Surface Resistance Test: " +
        $"{(externalSurfacePassed ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"Total Resistance Test: " +
        $"{(resistancePassed ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"U-Value Test: " +
        $"{(uValuePassed ? "PASS" : "FAIL")}");


    bool allTestsPassed =
        layerResistancePassed &&
        internalSurfacePassed &&
        externalSurfacePassed &&
        resistancePassed &&
        uValuePassed;

    Console.WriteLine();

    Console.WriteLine(
        allTestsPassed
            ? "SURFACE RESISTANCE TEST: SUCCESS"
            : "SURFACE RESISTANCE TEST: FAILED");
}
else
{
    Console.WriteLine("\nSURFACE RESISTANCE TEST: INVALID");

    foreach (var error in thermalResult.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }
}


// ============================================
// 8. COMPLETION
// ============================================

Console.WriteLine("\n========================================");
Console.WriteLine("SPECSYNTH THERMAL ENGINE TEST COMPLETE");
Console.WriteLine("========================================");