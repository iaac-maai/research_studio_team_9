using Autodesk.Revit.DB;

using SpecSynthAI.Models;

using ThermalEngine.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecSynthAI.Services
{
    public class ProjectWallCollectionService
    {
        private readonly RevitWallExtractionService _extractionService;

        public ProjectWallCollectionService()
        {
            _extractionService = new RevitWallExtractionService();
        }

        // ==========================================
        // COLLECT ALL PLACED WALLS
        // ==========================================

        public ProjectWallCollectionResult Collect(
            Document document,
            ProductCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(catalog);

            var result = new ProjectWallCollectionResult
            {
                ProjectName = document.Title
            };

            // STEP 1: Collect all placed wall instances.

            List<Wall> walls = new FilteredElementCollector(document)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(wall => wall.IsValidObject)
                .ToList();

            result.TotalPlacedWalls = walls.Count;

            // STEP 2: Group instances by Wall Type ID.

            var wallTypeGroups = walls
                .GroupBy(wall => wall.WallType.Id.Value)
                .ToList();

            result.UniqueWallTypes = wallTypeGroups.Count;

            // STEP 3: Process each unique wall type once.

            foreach (var group in wallTypeGroups)
            {
                Wall representativeWall = group.First();

                WallType wallType = representativeWall.WallType;

                var record = new ProjectWallTypeRecord
                {
                    WallTypeId = group.Key.ToString(),

                    WallTypeName = wallType.Name,

                    // Preserve the actual Revit wall function.
                    WallFunction = wallType.Function.ToString(),

                    InstanceCount = group.Count(),

                    InstanceIds = group
                        .Select(wall => wall.Id.Value.ToString())
                        .ToList()
                };

                try
                {
                    // STEP 4: Extract representative wall assembly.

                    WallAssembly assembly =
                        _extractionService.ExtractAssembly(
                            document,
                            representativeWall,
                            catalog);

                    record.Assembly = assembly;

                    record.ExtractionSuccessful = true;

                    record.Message =
                        "Extraction completed successfully.";

                    result.SuccessfulExtractions++;
                }
                catch (Exception ex)
                {
                    record.ExtractionSuccessful = false;

                    record.Message = ex.Message;

                    result.FailedExtractions++;
                }

                result.WallTypes.Add(record);
            }

            return result;
        }
    }


    // ==============================================
    // PROJECT COLLECTION RESULT
    // ==============================================

    public class ProjectWallCollectionResult
    {
        public string ProjectName { get; set; } = "";

        public int TotalPlacedWalls { get; set; }

        public int UniqueWallTypes { get; set; }

        public int SuccessfulExtractions { get; set; }

        public int FailedExtractions { get; set; }

        public List<ProjectWallTypeRecord> WallTypes { get; set; }
            = new();
    }


    // ==============================================
    // UNIQUE WALL TYPE RECORD
    // ==============================================

    public class ProjectWallTypeRecord
    {
        public string WallTypeId { get; set; } = "";

        public string WallTypeName { get; set; } = "";

        // Revit WallType.Function:
        // Interior, Exterior, Foundation, Retaining,
        // Soffit, CoreShaft, etc.
        public string WallFunction { get; set; } = "";

        public int InstanceCount { get; set; }

        public List<string> InstanceIds { get; set; } = new();

        public bool ExtractionSuccessful { get; set; }

        public string Message { get; set; } = "";

        public WallAssembly? Assembly { get; set; }

        public WallComparisonResult? ThermalPerformance
        {
            get;
            set;
        }
    }
}