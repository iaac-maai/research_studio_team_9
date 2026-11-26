using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ThermalEngine.Models;
using ThermalEngine.UI.Models;

namespace ThermalEngine.UI.Services;

public class ProjectWallJsonImportService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectWallJsonImportService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public ProjectWallImportResult ImportProject(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("JSON file path cannot be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project wall export JSON file was not found.", filePath);

        string json = File.ReadAllText(filePath);

        ProjectWallExportDto? project =
            JsonSerializer.Deserialize<ProjectWallExportDto>(json, _jsonOptions);

        if (project == null)
            throw new InvalidDataException("Unable to read the project wall export.");

        if (project.WallTypes == null || project.WallTypes.Count == 0)
            throw new InvalidDataException("The JSON file contains no wall records.");

        // Reconstruct thermal comparison results.
        var results = new List<WallComparisonResult>();

        foreach (var wall in project.WallTypes)
        {
            WallComparisonResult result = wall.ThermalPerformance ??
                new WallComparisonResult
                {
                    AssemblyName = wall.WallTypeName,
                    IsValid = false,
                    Errors = new List<string>
                    {
                        wall.ExtractionError ?? "Thermal performance data is unavailable."
                    }
                };

            results.Add(result);
        }

        List<WallComparisonResult> rankedResults = results
            .OrderBy(result => result.IsValid ? 0 : 1)
            .ThenBy(result => result.UValue ?? double.MaxValue)
            .ToList();

        var baselineRecord = project.WallTypes
            .FirstOrDefault(wall => wall.ThermalPerformance?.IsValid == true);

        var report = new WallComparisonReport
        {
            Results = rankedResults,
            BestPerformingAssembly = project.BestPerformingAssembly,
            LowestUValue = project.LowestUValue,
            BaselineAssembly = baselineRecord?.ThermalPerformance?.AssemblyName,
            BaselineUValue = baselineRecord?.ThermalPerformance?.UValue
        };

        // Restore classification metadata directly from JSON.
        // Missing properties in legacy exports retain safe model defaults.
        var wallRecords = project.WallTypes.Select(wall =>
            new ProjectWallRecord
            {
                WallTypeName = wall.WallTypeName,
                WallFunction = wall.WallFunction,
                WallTypeId = wall.WallTypeId,
                InstanceCount = wall.InstanceCount,
                RepresentativeWallId = wall.RepresentativeWallId,
                InstanceIds = wall.InstanceIds ?? new(),
                ThermalPerformance = wall.ThermalPerformance,
                ExtractionError = wall.ExtractionError,

                ThermalClassificationStatus =
                    string.IsNullOrWhiteSpace(wall.ThermalClassificationStatus)
                        ? "NOT ANALYSED"
                        : wall.ThermalClassificationStatus,

                ThermalTargetUValue = wall.ThermalTargetUValue,
                ThermalModerateLimit = wall.ThermalModerateLimit,

                ThermalClassificationExplanation =
                    string.IsNullOrWhiteSpace(wall.ThermalClassificationExplanation)
                        ? "Classification data was not included in this export."
                        : wall.ThermalClassificationExplanation,

                Layers = wall.Assembly?.Layers?
                    .Select(layer => new ProjectMaterialLayer
                    {
                        LayerName = layer.LayerName,
                        ThicknessMm = layer.ThicknessMm,
                        ThermalConductivityWmK = layer.ThermalConductivityWmK,
                        ConductivitySource = layer.ConductivitySource
                    }).ToList() ?? new()
            }).ToList();

        return new ProjectWallImportResult
        {
            ProjectName = project.ProjectName,
            ExportDate = project.ExportDate,
            TotalPlacedWalls = project.TotalPlacedWalls,
            UniqueWallTypes = project.UniqueWallTypes,
            SuccessfulExtractions = project.SuccessfulExtractions,
            FailedExtractions = project.FailedExtractions,
            Report = report,
            WallTypes = wallRecords
        };
    }

    // JSON DTOs. New fields are nullable/default-compatible with older exports.
    private class ProjectWallExportDto
    {
        public string ProjectName { get; set; } = "";
        public DateTime ExportDate { get; set; }
        public int TotalPlacedWalls { get; set; }
        public int UniqueWallTypes { get; set; }
        public int SuccessfulExtractions { get; set; }
        public int FailedExtractions { get; set; }
        public string? BestPerformingAssembly { get; set; }
        public double? LowestUValue { get; set; }
        public List<ProjectWallRecordDto> WallTypes { get; set; } = new();
    }

    private class ProjectWallRecordDto
    {
        public string WallTypeName { get; set; } = "";
        public string WallFunction { get; set; } = "";
        public long WallTypeId { get; set; }
        public int InstanceCount { get; set; }
        public long RepresentativeWallId { get; set; }
        public List<long> InstanceIds { get; set; } = new();
        public ProjectWallAssemblyDto? Assembly { get; set; }
        public WallComparisonResult? ThermalPerformance { get; set; }
        public string? ExtractionError { get; set; }

        public string? ThermalClassificationStatus { get; set; }
        public double? ThermalTargetUValue { get; set; }
        public double? ThermalModerateLimit { get; set; }
        public string? ThermalClassificationExplanation { get; set; }
    }

    private class ProjectWallAssemblyDto
    {
        public string AssemblyName { get; set; } = "";
        public List<ProjectLayerDto> Layers { get; set; } = new();
    }

    private class ProjectLayerDto
    {
        public string LayerName { get; set; } = "";
        public double ThicknessMm { get; set; }
        public double? ThermalConductivityWmK { get; set; }
        public int ConductivitySource { get; set; }
    }
}
