using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SpecSynthAI.Models;
using SpecSynthAI.Requirements;
using SpecSynthAI.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Interop;

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
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("SpecSynth AI", "No active Revit document is available.");
                    return Result.Failed;
                }

                string assemblyDirectory =
                    Path.GetDirectoryName(typeof(InspectElementCommand).Assembly.Location) ?? "";

                string catalogPath = Path.Combine(assemblyDirectory, "products.json");

                if (!File.Exists(catalogPath))
                {
                    TaskDialog.Show("SpecSynth AI", "Product catalogue not found:\n\n" + catalogPath);
                    return Result.Failed;
                }

                ProductCatalog catalog = ProductCatalog.Load(catalogPath);

                Reference pickedReference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall to inspect.");

                Element element = uiDoc.Document.GetElement(pickedReference);

                if (element is not Wall wall)
                {
                    TaskDialog.Show("SpecSynth AI", "Please select a wall.");
                    return Result.Cancelled;
                }

                WallType wallType = wall.WallType;
                CompoundStructure? structure = wallType.GetCompoundStructure();

                if (structure == null)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "The selected wall type has no compound structure.");
                    return Result.Failed;
                }

                var results = new List<InspectionLayerResult>();
                ProjectRequirement thermalRequirement =
                    RequirementDefaults.ThermalConductivity;

                foreach (CompoundStructureLayer layer in structure.GetLayers())
                {
                    if (layer.Function != MaterialFunctionAssignment.Insulation)
                        continue;

                    Material? material =
                        uiDoc.Document.GetElement(layer.MaterialId) as Material;

                    string materialName = material?.Name ?? "No material assigned";

                    double thicknessMm = UnitUtils.ConvertFromInternalUnits(
                        layer.Width,
                        UnitTypeId.Millimeters);

                    Product? product =
                        catalog.FindByRevitMaterialName(materialName);

                    string thicknessMatch =
                        ProductCatalog.CheckThicknessMatch(product, thicknessMm);

                    string thermalCheck =
                        ComplianceChecker.CheckThermalConductivity(
                            product,
                            thermalRequirement);

                    results.Add(new InspectionLayerResult
                    {
                        MaterialName = materialName,
                        InstalledThicknessMm = thicknessMm,
                        ProductMapping = product == null
                            ? "UNMAPPED"
                            : product.ProductId ?? "Matched product",
                        ThicknessMatch = thicknessMatch,
                        ThermalResult = thermalCheck,
                        Manufacturer = product?.Manufacturer ?? "Not available",
                        ProductName = product?.ProductName ?? "Not available",
                        ProductId = product?.ProductId ?? "Not available",
                        VerificationStatus = product?.VerificationStatus ?? "UNMAPPED",
                        CatalogueThicknessMm = product?.ThicknessMm,
                        ThermalConductivityWmK = product?.ThermalConductivityWmK,
                        AvailableThicknesses = FormatThicknesses(product),
                        SourceDocument = product?.SourceDocument ?? "",
                        SourceUrl = product?.SourceUrl ?? ""
                    });
                }

                var dialog = new InspectionResultsDialog(
                    wallType.Name,
                    catalog.Products.Count,
                    thermalRequirement.MaximumAllowedValue,
                    results);

                new WindowInteropHelper(dialog).Owner =
                    commandData.Application.MainWindowHandle;

                dialog.ShowDialog();

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
                    "Insulation inspection failed:\n\n" + ex.Message);

                return Result.Failed;
            }
        }

        private static string FormatThicknesses(Product? product)
        {
            if (product?.AvailableThicknessesMm == null ||
                product.AvailableThicknessesMm.Count == 0)
                return "Not documented";

            return string.Join(", ", product.AvailableThicknessesMm);
        }
    }
}
