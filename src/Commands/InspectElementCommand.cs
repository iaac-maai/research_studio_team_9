using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SpecSynthAI.Models;
using SpecSynthAI.Requirements;
using System;
using System.IO;
using System.Text;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class InspectElementCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIDocument uiDoc =
                    commandData.Application.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "No active Revit document is available.");

                    return Result.Failed;
                }

                string assemblyDirectory =
                    Path.GetDirectoryName(
                        typeof(InspectElementCommand).Assembly.Location)
                    ?? "";

                string catalogPath = Path.Combine(
                    assemblyDirectory,
                    "products.json");

                if (!File.Exists(catalogPath))
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "Product catalogue not found:\n\n" +
                        catalogPath);

                    return Result.Failed;
                }

                ProductCatalog catalog =
                    ProductCatalog.Load(catalogPath);

                Reference pickedReference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a wall to inspect.");

                Element element =
                    uiDoc.Document.GetElement(pickedReference);

                if (element is not Wall wall)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "Please select a wall.");

                    return Result.Cancelled;
                }

                WallType wallType = wall.WallType;

                CompoundStructure? structure =
                    wallType.GetCompoundStructure();

                if (structure == null)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "The selected wall type has no compound structure.");

                    return Result.Failed;
                }

                var report = new StringBuilder();

                report.AppendLine("--- INSULATION INSPECTION ---");
                report.AppendLine();
                report.AppendLine($"Wall Type: {wallType.Name}");
                report.AppendLine(
                    $"Products Loaded: {catalog.Products.Count}");
                report.AppendLine();

                bool insulationFound = false;

                foreach (CompoundStructureLayer layer
                    in structure.GetLayers())
                {
                    if (layer.Function !=
                        MaterialFunctionAssignment.Insulation)
                    {
                        continue;
                    }

                    insulationFound = true;

                    Material? material =
                        uiDoc.Document.GetElement(layer.MaterialId)
                        as Material;

                    string materialName =
                        material?.Name ?? "No material assigned";

                    double thicknessMm =
                        UnitUtils.ConvertFromInternalUnits(
                            layer.Width,
                            UnitTypeId.Millimeters);

                    // Data-driven material mapping.
                    Product? product =
                        catalog.FindByRevitMaterialName(materialName);

                    string thicknessMatch =
                        ProductCatalog.CheckThicknessMatch(
                            product,
                            thicknessMm);

                    string thermalCheck =
                        ComplianceChecker.CheckThermalConductivity(
                            product,
                            RequirementDefaults.ThermalConductivity);

                    report.AppendLine("--- INSULATION LAYER ---");
                    report.AppendLine($"Material: {materialName}");
                    report.AppendLine(
                        $"Installed Thickness: {thicknessMm:F1} mm");

                    report.AppendLine(
                        product == null
                            ? "Product Mapping: UNMAPPED"
                            : $"Product Mapping: {product.ProductId}");

                    report.AppendLine(
                        $"Thickness Match: {thicknessMatch}");

                    report.AppendLine(
                        $"Thermal Check: {thermalCheck}");

                    if (product != null)
                    {
                        report.AppendLine();
                        report.AppendLine(
                            "--- MATCHED CATALOGUE PRODUCT ---");

                        report.AppendLine(
                            $"Manufacturer: {product.Manufacturer}");

                        report.AppendLine(
                            $"Product: {product.ProductName}");

                        report.AppendLine(
                            $"Product ID: {product.ProductId}");

                        report.AppendLine(
                            $"Verification Status: " +
                            $"{product.VerificationStatus}");

                        report.AppendLine(
                            $"Catalogue Thickness: " +
                            $"{product.ThicknessMm:F1} mm");

                        report.AppendLine(
                            $"Thermal Conductivity: " +
                            $"{FormatConductivity(product.ThermalConductivityWmK)}");

                        report.AppendLine(
                            $"Available Thicknesses: " +
                            $"{FormatThicknesses(product)}");
                    }

                    report.AppendLine();
                }

                if (!insulationFound)
                {
                    report.AppendLine(
                        "No insulation layers were identified.");
                }

                report.AppendLine(
                    "Note: Material mapping and thickness matching do not " +
                    "prove complete product or assembly compatibility.");

                TaskDialog.Show(
                    "SpecSynth AI - Insulation Inspection",
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
                    "Insulation inspection failed:\n\n" +
                    ex.Message);

                return Result.Failed;
            }
        }

        private static string FormatConductivity(double? value)
        {
            if (!value.HasValue)
            {
                return "Not available";
            }

            return $"{value.Value:F3} W/(m·K)";
        }

        private static string FormatThicknesses(Product product)
        {
            if (product.AvailableThicknessesMm == null ||
                product.AvailableThicknessesMm.Count == 0)
            {
                return "Not documented";
            }

            return string.Join(
                ", ",
                product.AvailableThicknessesMm);
        }
    }
}