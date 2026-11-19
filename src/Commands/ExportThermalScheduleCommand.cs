using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using SpecSynthAI.Models;
using SpecSynthAI.Requirements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportThermalScheduleCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                Document doc =
                    commandData.Application.ActiveUIDocument.Document;

                // Locate the product catalogue beside the compiled DLL.
                string catalogPath = Path.Combine(
                    Path.GetDirectoryName(
                        typeof(ExportThermalScheduleCommand)
                            .Assembly.Location
                    )!,
                    "products.json"
                );

                if (!File.Exists(catalogPath))
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        $"Product catalogue not found:\n{catalogPath}"
                    );

                    return Result.Failed;
                }

                ProductCatalog catalog =
                    ProductCatalog.Load(catalogPath);

                var rows = new List<object[]>();

                // Collect unique wall types used in the Revit model.
                var wallTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Select(wall => doc.GetElement(wall.GetTypeId()))
                    .OfType<WallType>()
                    .GroupBy(wallType => wallType.Id.Value)
                    .Select(group => group.First())
                    .ToList();

                foreach (WallType wallType in wallTypes)
                {
                    CompoundStructure? structure =
                        wallType.GetCompoundStructure();

                    if (structure == null)
                    {
                        continue;
                    }

                    IList<CompoundStructureLayer> layers =
                        structure.GetLayers();

                    for (int index = 0; index < layers.Count; index++)
                    {
                        CompoundStructureLayer layer = layers[index];

                        // Inspect insulation layers only.
                        if (layer.Function !=
                            MaterialFunctionAssignment.Insulation)
                        {
                            continue;
                        }

                        Material? material =
                            doc.GetElement(layer.MaterialId) as Material;

                        string materialName =
                            material?.Name ?? "No material assigned";

                        double thicknessMm =
                            UnitUtils.ConvertFromInternalUnits(
                                layer.Width,
                                UnitTypeId.Millimeters
                            );

                        // Find the product using its configured Revit material name.
                        Product? product =
                            catalog.FindByRevitMaterialName(materialName);

                        // Check installed thickness against documented options.
                        string thicknessMatch =
                            ProductCatalog.CheckThicknessMatch(
                                product,
                                thicknessMm
                            );

                        // Check thermal conductivity against the project requirement.
                        string thermalCheck = "CANNOT VERIFY";

                        if (product != null)
                        {
                            thermalCheck =
                                ComplianceChecker.CheckThermalConductivity(
                                    product,
                                    RequirementDefaults.ThermalConductivity
                                );
                        }

                        // Preserve missing values as blank Excel cells.
                        object productThickness =
                            product != null && product.ThicknessMm > 0
                                ? (object)product.ThicknessMm
                                : "";

                        object conductivity =
                            product?.ThermalConductivityWmK.HasValue == true
                                ? (object)product.ThermalConductivityWmK.Value
                                : "";

                        object compressiveStrength =
                            product != null &&
                            product.CompressiveStrengthKPa > 0
                                ? (object)product.CompressiveStrengthKPa
                                : "";

                        string availableThicknesses =
                            product?.AvailableThicknessesMm != null
                                ? string.Join(
                                    ", ",
                                    product.AvailableThicknessesMm
                                        .Select(value =>
                                            value.ToString("0.##"))
                                )
                                : "";

                        string verificationStatus =
                            product?.VerificationStatus ?? "UNMAPPED";

                        // Determine the overall result conservatively.
                        string overallResult = CalculateOverallResult(
                            product,
                            verificationStatus,
                            thicknessMatch,
                            thermalCheck
                        );

                        rows.Add(new object[]
                        {
                            wallType.Name,
                            wallType.Id.Value,
                            wallType.UniqueId,
                            index,
                            layer.Function.ToString(),
                            materialName,
                            Math.Round(thicknessMm, 2),
                            product?.ProductId ?? "",
                            product?.Manufacturer ?? "",
                            product?.ProductName ?? "",
                            productThickness,
                            availableThicknesses,
                            product?.MaterialFamily ?? "",
                            conductivity,
                            compressiveStrength,
                            product?.ReactionToFire ?? "",
                            verificationStatus,
                            thicknessMatch,
                            thermalCheck,
                            product?.SourceDocument ?? "",
                            product?.SourceUrl ?? "",
                            overallResult
                        });
                    }
                }

                if (rows.Count == 0)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "No insulation layers were found in the wall types."
                    );

                    return Result.Succeeded;
                }

                // Ask the user where to save the Excel report.
                var dialog = new FileSaveDialog(
                    "Excel Workbook (*.xlsx)|*.xlsx"
                );

                dialog.Title = "Export Thermal Insulation Data";

                dialog.InitialFileName =
                    "SpecSynth_Thermal_Schedule.xlsx";

                if (dialog.Show() != ItemSelectionDialogResult.Confirmed)
                {
                    return Result.Cancelled;
                }

                string filePath =
                    ModelPathUtils.ConvertModelPathToUserVisiblePath(
                        dialog.GetSelectedModelPath()
                    );

                using var workbook = new XLWorkbook();

                var sheet =
                    workbook.Worksheets.Add("Thermal Schedule");

                // Excel column headings.
                string[] headers =
                {
                    "WallTypeName",
                    "WallTypeId",
                    "WallTypeUniqueId",
                    "LayerIndex",
                    "LayerFunction",
                    "MaterialName",
                    "LayerThickness_mm",
                    "ProductId",
                    "Manufacturer",
                    "ProductName",
                    "ProductThickness_mm",
                    "AvailableThicknesses_mm",
                    "MaterialFamily",
                    "ThermalConductivity_W_mK",
                    "CompressiveStrength_kPa",
                    "ReactionToFire",
                    "VerificationStatus",
                    "ThicknessMatch",
                    "ThermalCheck",
                    "SourceDocument",
                    "SourceUrl",
                    "OverallResult"
                };

                for (int col = 0; col < headers.Length; col++)
                {
                    sheet.Cell(1, col + 1).Value = headers[col];
                }

                // Write data rows.
                for (int row = 0; row < rows.Count; row++)
                {
                    for (int col = 0; col < headers.Length; col++)
                    {
                        object value = rows[row][col];

                        if (value is int integer)
                        {
                            sheet.Cell(row + 2, col + 1).Value = integer;
                        }
                        else if (value is long longValue)
                        {
                            sheet.Cell(row + 2, col + 1).Value = longValue;
                        }
                        else if (value is double doubleValue)
                        {
                            sheet.Cell(row + 2, col + 1).Value = doubleValue;
                        }
                        else
                        {
                            sheet.Cell(row + 2, col + 1).Value =
                                value?.ToString() ?? "";
                        }
                    }
                }

                // Format the report.
                var header = sheet.Range(
                    1,
                    1,
                    1,
                    headers.Length
                );

                header.Style.Font.Bold = true;

                sheet.SheetView.FreezeRows(1);
                sheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);

                TaskDialog.Show(
                    "SpecSynth AI",
                    "Thermal schedule exported successfully.\n\n" +
                    $"Wall types checked: {wallTypes.Count}\n" +
                    $"Insulation layers exported: {rows.Count}\n\n" +
                    $"File:\n{filePath}"
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "SpecSynth AI",
                    $"Export failed:\n{ex.Message}"
                );

                return Result.Failed;
            }
        }

        /// <summary>
        /// Produces a conservative summary of the checks currently implemented.
        /// This does not establish full wall assembly compliance.
        /// </summary>
        private static string CalculateOverallResult(
            Product? product,
            string verificationStatus,
            string thicknessMatch,
            string thermalCheck)
        {
            if (product == null)
            {
                return "CANNOT VERIFY";
            }

            if (!string.Equals(
                verificationStatus?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase))
            {
                return "CANNOT VERIFY";
            }

            // A confirmed failure in either implemented check means FAIL.
            if (string.Equals(
                    thermalCheck,
                    "FAIL",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    thicknessMatch,
                    "NO EXACT MATCH",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "FAIL";
            }

            // Both checks must be conclusive before returning PASS.
            bool thicknessPassed = string.Equals(
                thicknessMatch,
                "EXACT MATCH",
                StringComparison.OrdinalIgnoreCase
            );

            bool thermalPassed = string.Equals(
                thermalCheck,
                "PASS",
                StringComparison.OrdinalIgnoreCase
            );

            if (thicknessPassed && thermalPassed)
            {
                return "PASS";
            }

            return "CANNOT VERIFY";
        }
    }
}