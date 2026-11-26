using ThermalEngine.Models;
using ThermalEngine.Services;

namespace ThermalEngine.UI.Services;

public class PerformanceDashboardService
{
    private readonly WallComparisonService _comparisonService;

    public PerformanceDashboardService()
    {
        // Connect to the existing thermal comparison engine.
        _comparisonService = new WallComparisonService();
    }

    public WallComparisonReport GenerateReport()
    {
        // STEP 1: Define the wall assemblies.
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

        // STEP 2: Send the assemblies to the existing comparison engine.
        WallComparisonReport report =
            _comparisonService.CompareAssemblies(assemblies);

        // STEP 3: Return the calculated report.
        return report;
    }
}