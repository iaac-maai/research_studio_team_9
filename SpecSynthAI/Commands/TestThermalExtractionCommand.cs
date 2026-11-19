using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

using ThermalEngine.Models;

using System;
using System.IO;
using System.Text;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class TestThermalExtractionCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // ==========================================
            // STEP 0: T01 THERMAL ENGINE VALIDATION
            // ==========================================

            ThermalEngineConnectionTest.Test();

            TaskDialog.Show(
                "SpecSynth AI - T01 Validation",
                "T01 thermal engine validation passed.\n\n" +
                "Reference wall calculation matches the expected values.");

            // ==========================================
            // STEP 1: ACCESS ACTIVE REVIT DOCUMENT
            // ==========================================

            UIDocument uiDoc =
                commandData.Application.ActiveUIDocument;

            Document doc = uiDoc.Document;

            // ==========================================
            // STEP 2: LOCATE products.json
            // ==========================================

            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(TestThermalExtractionCommand)
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

            // ==========================================
            // STEP 3: LOAD PRODUCT CATALOGUE
            // ==========================================

            ProductCatalog catalog =
                ProductCatalog.Load(catalogPath);

            // ==========================================
            // STEP 4: SELECT WALL
            // ==========================================

            Reference reference =
                uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall for thermal extraction.");

            Element element = doc.GetElement(reference);

            if (element is not Wall wall)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "Selected element is not a wall.");

                return Result.Cancelled;
            }

            // ==========================================
            // STEP 5: EXTRACT WALL ASSEMBLY
            // ==========================================

            var extractionService =
                new RevitWallExtractionService();

            WallAssembly assembly =
                extractionService.ExtractAssembly(
                    doc,
                    wall,
                    catalog);

            // ==========================================
            // STEP 6: PREPARE EXTRACTION REPORT
            // ==========================================

            var report = new StringBuilder();

            report.AppendLine("THERMAL EXTRACTION REPORT");
            report.AppendLine("------------------------------");

            report.AppendLine($"Wall: {wall.WallType.Name}");
            report.AppendLine($"Wall ID: {wall.Id.Value}");
            report.AppendLine($"Total Layers: {assembly.Layers.Count}");

            report.AppendLine();
            report.AppendLine("LAYER INFORMATION");
            report.AppendLine("------------------------------");

            int manufacturerCount = 0;
            int revitAssetCount = 0;
            int unknownCount = 0;

            // ==========================================
            // STEP 7: DISPLAY EACH LAYER
            // ==========================================

            for (int i = 0; i < assembly.Layers.Count; i++)
            {
                ThermalLayer layer = assembly.Layers[i];

                report.AppendLine();
                report.AppendLine($"Layer {i + 1}: {layer.LayerName}");

                report.AppendLine(
                    $"Thickness: {layer.ThicknessMm:F2} mm");

                report.AppendLine(
                    $"Conductivity: " +
                    (layer.ThermalConductivityWmK.HasValue
                        ? $"{layer.ThermalConductivityWmK.Value:F4} W/(m·K)"
                        : "NOT AVAILABLE"));

                string sourceDescription =
                    layer.ConductivitySource switch
                    {
                        ConductivitySource.VerifiedManufacturerProduct =>
                            "VERIFIED MANUFACTURER PRODUCT",

                        ConductivitySource.RevitThermalAsset =>
                            "REVIT THERMAL ASSET",

                        _ =>
                            "UNKNOWN / NOT AVAILABLE"
                    };

                report.AppendLine(
                    $"Data Source: {sourceDescription}");

                switch (layer.ConductivitySource)
                {
                    case ConductivitySource.VerifiedManufacturerProduct:
                        manufacturerCount++;
                        break;

                    case ConductivitySource.RevitThermalAsset:
                        revitAssetCount++;
                        break;

                    default:
                        unknownCount++;
                        break;
                }
            }

            // ==========================================
            // STEP 8: EXTRACTION SUMMARY
            // ==========================================

            report.AppendLine();
            report.AppendLine("------------------------------");

            report.AppendLine(
                $"Manufacturer Verified: {manufacturerCount}");

            report.AppendLine(
                $"Revit Thermal Assets: {revitAssetCount}");

            report.AppendLine(
                $"Unknown Conductivity: {unknownCount}");

            report.AppendLine();

            report.AppendLine(
                unknownCount == 0
                    ? "EXTRACTION STATUS: COMPLETE"
                    : "EXTRACTION STATUS: PARTIAL");

            // ==========================================
            // STEP 9: DISPLAY REPORT
            // ==========================================

            TaskDialog.Show(
                "SpecSynth AI - Thermal Extraction",
                report.ToString());

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.ToString();

            TaskDialog.Show(
                "SpecSynth AI",
                "Thermal extraction failed:\n\n" + ex.Message);

            return Result.Failed;
        }
    }
}