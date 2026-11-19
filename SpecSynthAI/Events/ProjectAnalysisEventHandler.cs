using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

using ThermalEngine.Models;
using ThermalEngine.Services;
using ThermalEngine.UI.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpecSynthAI.Events
{
    public class ProjectAnalysisEventHandler : IExternalEventHandler
    {
        private readonly RevitWallExtractionService _extractionService;
        private readonly ThermalCalculator _thermalCalculator;
        private readonly ProjectThermalClassificationService _classificationService;

        private Action<ProjectAnalysisResult>? _completed;

        public ProjectAnalysisEventHandler()
        {
            _extractionService = new RevitWallExtractionService();
            _thermalCalculator = new ThermalCalculator();
            _classificationService = new ProjectThermalClassificationService();
        }

        public void RequestAnalysis(Action<ProjectAnalysisResult> completed)
        {
            _completed = completed ??
                throw new ArgumentNullException(nameof(completed));
        }

        public void Execute(UIApplication application)
        {
            ProjectAnalysisResult result;

            try
            {
                UIDocument? uiDocument = application.ActiveUIDocument;

                if (uiDocument == null)
                {
                    result = ProjectAnalysisResult.Failed(
                        "No active Revit document is available.");
                    Complete(result);
                    return;
                }

                Document document = uiDocument.Document;

                // 1. Load product catalogue.
                string assemblyDirectory =
                    Path.GetDirectoryName(
                        typeof(ProjectAnalysisEventHandler).Assembly.Location) ?? "";

                string catalogPath = Path.Combine(assemblyDirectory, "products.json");

                if (!File.Exists(catalogPath))
                {
                    result = ProjectAnalysisResult.Failed(
                        "Product catalogue not found:\n" + catalogPath);
                    Complete(result);
                    return;
                }

                ProductCatalog catalog = ProductCatalog.Load(catalogPath);

                // 2. Collect placed walls.
                List<Wall> walls = new FilteredElementCollector(document)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Where(wall => wall.WallType != null)
                    .ToList();

                if (walls.Count == 0)
                {
                    result = ProjectAnalysisResult.Failed(
                        "No placed walls were found in this project.");
                    Complete(result);
                    return;
                }

                // 3. Group walls by wall type and extract/calculate each assembly.
                var wallGroups = walls.GroupBy(wall => wall.WallType.Id.Value);
                var wallRecords = new List<ProjectWallExportRecord>();

                foreach (var group in wallGroups)
                {
                    Wall representativeWall = group.First();
                    WallType wallType = representativeWall.WallType;

                    var record = new ProjectWallExportRecord
                    {
                        WallTypeName = wallType.Name,
                        WallFunction = wallType.Function.ToString(),
                        WallTypeId = wallType.Id.Value,
                        InstanceCount = group.Count(),
                        RepresentativeWallId = representativeWall.Id.Value,
                        InstanceIds = group.Select(wall => wall.Id.Value).ToList()
                    };

                    try
                    {
                        WallAssembly assembly = _extractionService.ExtractAssembly(
                            document,
                            representativeWall,
                            catalog);

                        record.Assembly = new ProjectWallExportAssembly
                        {
                            AssemblyName = assembly.AssemblyName,
                            Layers = assembly.Layers.Select(layer =>
                                new ProjectWallExportLayer
                                {
                                    LayerName = layer.LayerName,
                                    ThicknessMm = layer.ThicknessMm,
                                    ThermalConductivityWmK = layer.ThermalConductivityWmK,
                                    ConductivitySource = (int)layer.ConductivitySource
                                }).ToList()
                        };

                        ThermalCalculationResult calculation =
                            _thermalCalculator.CalculateAssembly(assembly.Layers);

                        record.ThermalPerformance = new WallComparisonResult
                        {
                            AssemblyName = assembly.AssemblyName,
                            IsValid = calculation.IsValid,
                            LayerResistance = calculation.LayerResistance,
                            InternalSurfaceResistance = calculation.InternalSurfaceResistance,
                            ExternalSurfaceResistance = calculation.ExternalSurfaceResistance,
                            TotalResistance = calculation.TotalResistance,
                            UValue = calculation.UValue,
                            LayerResults = calculation.LayerResults.ToList(),
                            Errors = calculation.Errors.ToList()
                        };
                    }
                    catch (Exception ex)
                    {
                        record.ExtractionError = ex.Message;
                    }

                    wallRecords.Add(record);
                }

                // 4. Classify in the Revit analysis pipeline BEFORE JSON export.
                // The adapter uses the existing classification service and model.
                List<ThermalEngine.UI.Models.ProjectWallRecord> classificationRecords =
                    wallRecords.Select(record =>
                        new ThermalEngine.UI.Models.ProjectWallRecord
                        {
                            WallTypeName = record.WallTypeName,
                            WallFunction = record.WallFunction,
                            WallTypeId = record.WallTypeId,
                            InstanceCount = record.InstanceCount,
                            RepresentativeWallId = record.RepresentativeWallId,
                            InstanceIds = record.InstanceIds,
                            ThermalPerformance = record.ThermalPerformance,
                            ExtractionError = record.ExtractionError
                        }).ToList();

                ProjectThermalClassificationResult classification =
                    _classificationService.ClassifyProject(classificationRecords);

                // Copy all classification outputs onto the export DTO.
                // The same ordering is retained from wallRecords to adapter records.
                for (int index = 0; index < wallRecords.Count; index++)
                {
                    var classifiedRecord = classificationRecords[index];
                    var classificationResult = classification.Walls[index];

                    wallRecords[index].ThermalClassificationStatus =
                        classifiedRecord.ThermalClassificationStatus;

                    wallRecords[index].ThermalTargetUValue =
                        classifiedRecord.ThermalTargetUValue;

                    wallRecords[index].ThermalModerateLimit =
                        classificationResult.ModerateLimit;

                    wallRecords[index].ThermalClassificationExplanation =
                        classifiedRecord.ThermalClassificationExplanation;
                }

                // 5. Rank valid assemblies.
                var validResults = wallRecords
                    .Where(record =>
                        record.ThermalPerformance?.IsValid == true &&
                        record.ThermalPerformance.UValue.HasValue)
                    .OrderBy(record => record.ThermalPerformance!.UValue)
                    .ToList();

                foreach (var record in validResults)
                    record.ThermalPerformance!.ImprovementPercentage = 0;

                string? bestAssembly =
                    validResults.FirstOrDefault()?.ThermalPerformance?.AssemblyName;

                double? lowestUValue =
                    validResults.FirstOrDefault()?.ThermalPerformance?.UValue;

                // 6. Create export with classification metadata included per wall type.
                var export = new ProjectWallExport
                {
                    ProjectName = document.Title,
                    ExportDate = DateTime.Now,
                    TotalPlacedWalls = walls.Count,
                    UniqueWallTypes = wallRecords.Count,
                    SuccessfulExtractions = wallRecords.Count(record => record.Assembly != null),
                    FailedExtractions = wallRecords.Count(record => record.Assembly == null),
                    BestPerformingAssembly = bestAssembly,
                    LowestUValue = lowestUValue,
                    WallTypes = wallRecords
                };

                // 7. Save JSON.
                string exportDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SpecSynth_Exports");

                Directory.CreateDirectory(exportDirectory);

                string safeProjectName = string.Join(
                    "_",
                    document.Title.Split(Path.GetInvalidFileNameChars()));

                string filePath = Path.Combine(
                    exportDirectory,
                    $"{safeProjectName}_Performance_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                string json = JsonSerializer.Serialize(
                    export,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(filePath, json);

                result = new ProjectAnalysisResult
                {
                    Success = true,
                    JsonFilePath = filePath,
                    TotalWalls = walls.Count,
                    UniqueWallTypes = wallRecords.Count,
                    SuccessfulExtractions = export.SuccessfulExtractions,
                    FailedExtractions = export.FailedExtractions,
                    ValidCalculations = wallRecords.Count(
                        record => record.ThermalPerformance?.IsValid == true),
                    InvalidCalculations = wallRecords.Count(
                        record => record.ThermalPerformance != null &&
                                  !record.ThermalPerformance.IsValid),
                    Message = "Project analysis completed successfully."
                };
            }
            catch (Exception ex)
            {
                result = ProjectAnalysisResult.Failed(ex.Message);
            }

            Complete(result);
        }

        private void Complete(ProjectAnalysisResult result)
        {
            Action<ProjectAnalysisResult>? callback = _completed;
            _completed = null;

            if (callback != null)
            {
                try { callback(result); }
                catch { /* Prevent callback errors from escaping the Revit event. */ }
            }
        }

        public string GetName()
        {
            return "SpecSynth AI - Project Performance Analysis";
        }
    }

    public class ProjectAnalysisResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string JsonFilePath { get; set; } = "";
        public int TotalWalls { get; set; }
        public int UniqueWallTypes { get; set; }
        public int SuccessfulExtractions { get; set; }
        public int FailedExtractions { get; set; }
        public int ValidCalculations { get; set; }
        public int InvalidCalculations { get; set; }

        public static ProjectAnalysisResult Failed(string message)
        {
            return new ProjectAnalysisResult { Success = false, Message = message };
        }
    }

    // Export DTOs.
    public class ProjectWallExport
    {
        public string ProjectName { get; set; } = "";
        public DateTime ExportDate { get; set; }
        public int TotalPlacedWalls { get; set; }
        public int UniqueWallTypes { get; set; }
        public int SuccessfulExtractions { get; set; }
        public int FailedExtractions { get; set; }
        public string? BestPerformingAssembly { get; set; }
        public double? LowestUValue { get; set; }
        public List<ProjectWallExportRecord> WallTypes { get; set; } = new();
    }

    public class ProjectWallExportRecord
    {
        public string WallTypeName { get; set; } = "";
        public string WallFunction { get; set; } = "";
        public long WallTypeId { get; set; }
        public int InstanceCount { get; set; }
        public long RepresentativeWallId { get; set; }
        public List<long> InstanceIds { get; set; } = new();
        public ProjectWallExportAssembly? Assembly { get; set; }
        public WallComparisonResult? ThermalPerformance { get; set; }
        public string? ExtractionError { get; set; }

        // Classification snapshot persisted with the thermal result.
        public string ThermalClassificationStatus { get; set; } = "NOT ANALYSED";
        public double? ThermalTargetUValue { get; set; }
        public double? ThermalModerateLimit { get; set; }
        public string ThermalClassificationExplanation { get; set; } =
            "Classification has not been performed.";
    }

    public class ProjectWallExportAssembly
    {
        public string AssemblyName { get; set; } = "";
        public List<ProjectWallExportLayer> Layers { get; set; } = new();
    }

    public class ProjectWallExportLayer
    {
        public string LayerName { get; set; } = "";
        public double ThicknessMm { get; set; }
        public double? ThermalConductivityWmK { get; set; }
        public int ConductivitySource { get; set; }
    }
}
