using System;
using System.Collections.Generic;
using System.Linq;
using ThermalEngine.Models;

namespace ThermalEngine.UI.Models
{
    public class ProjectWallImportResult
    {
        public string ProjectName { get; set; } = "";
        public DateTime ExportDate { get; set; }
        public int TotalPlacedWalls { get; set; }
        public int UniqueWallTypes { get; set; }
        public int SuccessfulExtractions { get; set; }
        public int FailedExtractions { get; set; }
        public WallComparisonReport Report { get; set; } = new();
        public List<ProjectWallRecord> WallTypes { get; set; } = new();

        public int ValidAssemblies =>
            Report.Results.Count(result => result.IsValid);

        public int InvalidAssemblies =>
            Report.Results.Count(result => !result.IsValid);

        public int UnsupportedAssemblies =>
            WallTypes.Count(wall => wall.ThermalPerformance == null);
    }

    public class ProjectWallRecord
    {
        public string WallTypeName { get; set; } = "";
        public string WallFunction { get; set; } = "";
        public long WallTypeId { get; set; }
        public int InstanceCount { get; set; }
        public long RepresentativeWallId { get; set; }
        public List<long> InstanceIds { get; set; } = new();
        public WallComparisonResult? ThermalPerformance { get; set; }
        public string? ExtractionError { get; set; }
        public List<ProjectMaterialLayer> Layers { get; set; } = new();

        // Persisted thermal classification snapshot.
        public string ThermalClassificationStatus { get; set; } = "NOT ANALYSED";
        public double? ThermalTargetUValue { get; set; }
        public double? ThermalModerateLimit { get; set; }
        public string ThermalClassificationExplanation { get; set; } =
            "Classification has not been performed.";

        public double? CalculatedUValue => ThermalPerformance?.UValue;

        public string UValueDisplay =>
            CalculatedUValue.HasValue ? CalculatedUValue.Value.ToString("F3") : "—";

        public string TargetUValueDisplay =>
            ThermalTargetUValue.HasValue ? ThermalTargetUValue.Value.ToString("F3") : "—";

        public int PlacedInstanceCount => Math.Max(0, InstanceCount);

        public string InstanceCountDisplay => InstanceCount.ToString();

        public string WallFunctionDisplay =>
            string.IsNullOrWhiteSpace(WallFunction) ? "Unknown" : WallFunction;

        public bool IsExtracted => ExtractionError == null;

        public bool IsCalculationValid => ThermalPerformance?.IsValid == true;

        public string Status =>
            !IsExtracted ? "EXTRACTION FAILED" :
            IsCalculationValid ? "VALID" : "INVALID";
    }

    public class ProjectMaterialLayer
    {
        public string LayerName { get; set; } = "";
        public double ThicknessMm { get; set; }
        public double? ThermalConductivityWmK { get; set; }

        // 0 = Unknown; 1 = Verified Manufacturer Product; 2 = Revit Thermal Asset.
        public int ConductivitySource { get; set; }

        public string ConductivitySourceName =>
            ConductivitySource switch
            {
                1 => "Verified Manufacturer Product",
                2 => "Revit Thermal Asset",
                _ => "Unknown"
            };
    }
}
