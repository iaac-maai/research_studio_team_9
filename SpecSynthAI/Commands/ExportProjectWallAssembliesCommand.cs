using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

using ThermalEngine.Models;
using ThermalEngine.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class ExportProjectWallAssembliesCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // ============================================
            // STEP 1: ACCESS ACTIVE REVIT DOCUMENT
            // ============================================

            UIDocument uiDoc =
                commandData.Application.ActiveUIDocument;

            Document doc = uiDoc.Document;


            // ============================================
            // STEP 2: LOCATE PRODUCT CATALOGUE
            // ============================================

            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(ExportProjectWallAssembliesCommand)
                        .Assembly.Location) ?? "";

            string catalogPath =
                Path.Combine(assemblyDirectory, "products.json");

            if (!File.Exists(catalogPath))
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "Product catalogue not found:\n" + catalogPath);

                return Result.Failed;
            }

            ProductCatalog catalog =
                ProductCatalog.Load(catalogPath);


            // ============================================
            // STEP 3: COLLECT ALL PLACED WALLS
            // ============================================

            List<Wall> allWalls =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Where(wall => wall.WallType != null)
                    .ToList();

            if (allWalls.Count == 0)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "No placed walls were found in this project.");

                return Result.Cancelled;
            }


            // ============================================
            // STEP 4: GROUP WALLS BY WALL TYPE
            // ============================================

            var wallGroups = allWalls
                .GroupBy(wall => wall.WallType.Id.Value)
                .ToList();


            // ============================================
            // STEP 5: INITIALISE SERVICES
            // ============================================

            var extractionService =
                new RevitWallExtractionService();

            var comparisonService =
                new WallComparisonService();

            var exportRecords =
                new List<ProjectWallExportRecord>();

            var assemblies =
                new List<WallAssembly>();


            int extractionSuccessCount = 0;
            int extractionFailureCount = 0;


            // ============================================
            // STEP 6: EXTRACT EACH UNIQUE WALL TYPE
            // ============================================

            foreach (var group in wallGroups)
            {
                Wall representativeWall = group.First();

                WallType wallType =
                    representativeWall.WallType;

                try
                {
                    // Extract the wall construction.
                    WallAssembly assembly =
                        extractionService.ExtractAssembly(
                            doc,
                            representativeWall,
                            catalog);

                    assemblies.Add(assembly);

                    // Store BIM identification information.
                    exportRecords.Add(
                        new ProjectWallExportRecord
                        {
                            WallTypeName = wallType.Name,

                            WallTypeId = wallType.Id.Value,

                            InstanceCount = group.Count(),

                            RepresentativeWallId =
                                representativeWall.Id.Value,

                            InstanceIds = group
                                .Select(wall => wall.Id.Value)
                                .ToList(),

                            Assembly = assembly
                        });

                    extractionSuccessCount++;
                }
                catch (Exception ex)
                {
                    exportRecords.Add(
                        new ProjectWallExportRecord
                        {
                            WallTypeName = wallType.Name,

                            WallTypeId = wallType.Id.Value,

                            InstanceCount = group.Count(),

                            RepresentativeWallId =
                                representativeWall.Id.Value,

                            InstanceIds = group
                                .Select(wall => wall.Id.Value)
                                .ToList(),

                            ExtractionError = ex.Message
                        });

                    extractionFailureCount++;
                }
            }


            // ============================================
            // STEP 7: CALCULATE THERMAL PERFORMANCE
            // ============================================

            WallComparisonReport comparisonReport =
                comparisonService.CompareAssemblies(assemblies);


            // ============================================
            // STEP 8: CONNECT RESULTS TO WALL TYPES
            // ============================================

            var resultsByAssemblyName =
                comparisonReport.Results.ToDictionary(
                    result => result.AssemblyName,
                    result => result);

            foreach (var record in exportRecords)
            {
                if (record.Assembly == null)
                    continue;

                if (resultsByAssemblyName.TryGetValue(
                    record.Assembly.AssemblyName,
                    out WallComparisonResult? result))
                {
                    record.ThermalPerformance = result;
                }
            }


            // ============================================
            // STEP 9: CREATE EXPORT DOCUMENT
            // ============================================

            var exportDocument = new ProjectWallExportDocument
            {
                ProjectName = doc.Title,

                ExportDate = DateTime.Now,

                TotalPlacedWalls = allWalls.Count,

                UniqueWallTypes = wallGroups.Count,

                SuccessfulExtractions = extractionSuccessCount,

                FailedExtractions = extractionFailureCount,

                BestPerformingAssembly =
                    comparisonReport.BestPerformingAssembly,

                LowestUValue =
                    comparisonReport.LowestUValue,

                WallTypes = exportRecords
            };


            // ============================================
            // STEP 10: SERIALISE JSON
            // ============================================

            JsonSerializerOptions jsonOptions = new()
            {
                WriteIndented = true
            };

            string json =
                JsonSerializer.Serialize(
                    exportDocument,
                    jsonOptions);


            // ============================================
            // STEP 11: EXPORT FILE
            // ============================================

            string exportDirectory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "SpecSynth_Exports");

            Directory.CreateDirectory(exportDirectory);

            string timestamp =
                DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string exportPath =
                Path.Combine(
                    exportDirectory,
                    $"ProjectWallPerformance_{timestamp}.json");

            File.WriteAllText(exportPath, json);


            // ============================================
            // STEP 12: DISPLAY EXPORT SUMMARY
            // ============================================

            TaskDialog.Show(
                "SpecSynth AI - Project Wall Export",

                "PROJECT WALL EXTRACTION COMPLETE\n\n" +

                $"Project: {doc.Title}\n\n" +

                $"Placed Walls: {allWalls.Count}\n" +

                $"Unique Wall Types: {wallGroups.Count}\n\n" +

                $"Successful Extractions: {extractionSuccessCount}\n" +

                $"Failed Extractions: {extractionFailureCount}\n\n" +

                $"Calculated Assemblies: {comparisonReport.Results.Count}\n\n" +

                $"Best Performing Assembly:\n" +

                $"{comparisonReport.BestPerformingAssembly ?? "Not available"}\n\n" +

                $"Lowest U-Value: " +

                $"{comparisonReport.LowestUValue?.ToString("F4") ?? "N/A"} W/(m²·K)\n\n" +

                "JSON Export Location:\n" + exportPath
            );

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.ToString();

            TaskDialog.Show(
                "SpecSynth AI",
                "Project wall export failed:\n\n" + ex.Message);

            return Result.Failed;
        }
    }
}


// =====================================================
// PROJECT WALL EXPORT DATA STRUCTURES
// =====================================================

public class ProjectWallExportDocument
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

    public long WallTypeId { get; set; }

    public int InstanceCount { get; set; }

    public long RepresentativeWallId { get; set; }

    public List<long> InstanceIds { get; set; } = new();

    public WallAssembly? Assembly { get; set; }

    public WallComparisonResult? ThermalPerformance { get; set; }

    public string? ExtractionError { get; set; }
}