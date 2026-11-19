using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

using ThermalEngine.Models;
using ThermalEngine.Services;

using System;
using System.IO;
using System.Text;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class CalculateWallPerformanceCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // STEP 1: Get the active Revit document.
            UIDocument uiDoc =
                commandData.Application.ActiveUIDocument;

            Document doc = uiDoc.Document;

            // STEP 2: Locate the product catalogue.
            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(CalculateWallPerformanceCommand)
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

            // STEP 3: Load verified manufacturer products.
            ProductCatalog catalog =
                ProductCatalog.Load(catalogPath);

            // STEP 4: Ask the user to select a wall.
            Reference reference =
                uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall for thermal performance calculation.");

            Element element = doc.GetElement(reference);

            if (element is not Wall wall)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "Selected element is not a wall.");

                return Result.Cancelled;
            }

            // STEP 5: Extract the wall construction.
            var extractionService =
                new RevitWallExtractionService();

            WallAssembly assembly =
                extractionService.ExtractAssembly(
                    doc,
                    wall,
                    catalog);

            // STEP 6: Calculate thermal performance.
            var thermalCalculator =
                new ThermalCalculator();

            ThermalCalculationResult calculation =
                thermalCalculator.CalculateAssembly(
                    assembly.Layers);

            // STEP 7: Prepare the performance report.
            var report = new StringBuilder();

            report.AppendLine("WALL THERMAL PERFORMANCE REPORT");
            report.AppendLine("================================");
            report.AppendLine();

            report.AppendLine($"Wall Type: {wall.WallType.Name}");
            report.AppendLine($"Wall ID: {wall.Id.Value}");
            report.AppendLine($"Total Layers: {assembly.Layers.Count}");

            report.AppendLine();
            report.AppendLine("MATERIAL INFORMATION");
            report.AppendLine("--------------------------------");

            int manufacturerCount = 0;
            int revitAssetCount = 0;
            int unknownCount = 0;

            // STEP 8: Report each construction layer.
            for (int i = 0; i < assembly.Layers.Count; i++)
            {
                ThermalLayer layer = assembly.Layers[i];

                report.AppendLine();
                report.AppendLine($"Layer {i + 1}: {layer.LayerName}");

                report.AppendLine(
                    $"Thickness: {layer.ThicknessMm:F2} mm");

                report.AppendLine(
                    layer.ThermalConductivityWmK.HasValue
                        ? $"Conductivity: {layer.ThermalConductivityWmK.Value:F4} W/(m·K)"
                        : "Conductivity: NOT AVAILABLE");

                report.AppendLine(
                    $"Data Source: {layer.ConductivitySource}");

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

            // STEP 9: Display calculation results.
            report.AppendLine();
            report.AppendLine("================================");

            if (calculation.IsValid)
            {
                report.AppendLine("CALCULATION STATUS: SUCCESS");

                report.AppendLine();

                report.AppendLine(
                    $"Total Thermal Resistance: {calculation.TotalResistance:F4} m²·K/W");

                report.AppendLine(
                    $"Wall U-Value: {calculation.UValue:F4} W/(m²·K)");

                report.AppendLine();
                report.AppendLine("LAYER RESISTANCE CONTRIBUTIONS");
                report.AppendLine("--------------------------------");

                foreach (ThermalLayerResult layerResult
                    in calculation.LayerResults)
                {
                    report.AppendLine();

                    report.AppendLine(
                        $"Layer: {layerResult.LayerName}");

                    report.AppendLine(
                        $"Thermal Resistance: {layerResult.ThermalResistance:F4} m²·K/W");

                    report.AppendLine(
                        $"Resistance Contribution: {layerResult.ResistanceContributionPercentage:F2}%");
                }
            }
            else
            {
                report.AppendLine("CALCULATION STATUS: INCOMPLETE");

                report.AppendLine();
                report.AppendLine(
                    "A reliable U-value cannot be calculated because some material properties are missing or invalid.");

                report.AppendLine();
                report.AppendLine("CALCULATION ERRORS");
                report.AppendLine("--------------------------------");

                foreach (string error in calculation.Errors)
                {
                    report.AppendLine();
                    report.AppendLine("• " + error);
                }
            }

            // STEP 10: Display data-source summary.
            report.AppendLine();
            report.AppendLine("================================");

            report.AppendLine(
                $"Manufacturer Verified: {manufacturerCount}");

            report.AppendLine(
                $"Revit Thermal Assets: {revitAssetCount}");

            report.AppendLine(
                $"Unknown Conductivity: {unknownCount}");

            // STEP 11: Display the report.
            TaskDialog.Show(
                "SpecSynth AI - Wall Performance",
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
                "Wall performance calculation failed:\n\n" + ex.Message);

            return Result.Failed;
        }
    }
}