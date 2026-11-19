using System;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

namespace SpecSynthAI.Commands
{
    public class MaterialImportEventHandler : IExternalEventHandler
    {
        private Product? _selectedProduct;


        // =====================================================
        // 1. REQUEST IMPORT
        // =====================================================

        public void RequestImport(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            _selectedProduct = product;
        }


        // =====================================================
        // 2. EXECUTE IMPORT
        // =====================================================

        public void Execute(UIApplication application)
        {
            Product? product = _selectedProduct;

            _selectedProduct = null;

            if (product == null)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "No product was selected.");

                return;
            }

            UIDocument? uiDocument =
                application.ActiveUIDocument;

            if (uiDocument == null)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "No active Revit document.");

                return;
            }

            Document doc =
                uiDocument.Document;

            if (doc.IsReadOnly)
            {
                TaskDialog.Show(
                    "SpecSynth AI",
                    "The Revit document is read-only.");

                return;
            }


            // =================================================
            // 3. VALIDATE PRODUCT
            // =================================================

            MaterialImportValidationResult validation =
                RevitMaterialCreationService.ValidateProduct(product);

            if (!validation.IsValid)
            {
                TaskDialog.Show(
                    "SpecSynth AI - Validation Failed",
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));

                return;
            }


            // =================================================
            // 4. INSPECT EXISTING MATERIAL
            // =================================================

            MaterialInspectionResult inspection =
                RevitMaterialCreationService.InspectExistingMaterial(
                    doc,
                    product);

            if (inspection.Errors.Count > 0)
            {
                TaskDialog.Show(
                    "SpecSynth AI - Inspection Failed",
                    string.Join(
                        Environment.NewLine,
                        inspection.Errors));

                return;
            }


            // The product may already be linked.
            // In that case, continue so standard Revit Identity fields
            // can be synchronised without replacing its Thermal Asset.

            bool alreadyLinked =
                inspection.IsLinkedToSelectedProduct;


            // Material linked to another product.

            if (!alreadyLinked &&
                inspection.HasManufacturerMetadata &&
                !string.IsNullOrWhiteSpace(
                    inspection.ExistingProductId))
            {
                TaskDialog.Show(
                    "SpecSynth AI - Product Conflict",

                    "This material is already linked to another product.\n\n" +
                    $"Existing Product ID: {inspection.ExistingProductId}\n" +
                    $"Selected Product ID: {product.ProductId}\n\n" +
                    "Import cancelled to protect existing metadata.");

                return;
            }


            // =================================================
            // 5. CHECK EXISTING THERMAL ASSET
            // =================================================

            bool replaceThermalAsset = false;

            if (inspection.MaterialExists &&
                inspection.HasThermalAsset &&
                inspection.ThermalConductivityWmK.HasValue)
            {
                double existingValue =
                    inspection.ThermalConductivityWmK.Value;

                double requestedValue =
                    product.ThermalConductivityWmK!.Value;

                bool valuesMatch =
                    Math.Abs(existingValue - requestedValue)
                    < 0.000001;

                if (!valuesMatch)
                {
                    TaskDialog dialog = new TaskDialog(
                        "SpecSynth AI - Thermal Conflict");

                    dialog.MainInstruction =
                        "Existing Thermal Asset has different conductivity.";

                    dialog.MainContent =
                        $"Material: {inspection.MaterialName}\n\n" +
                        $"Existing: {existingValue:F4} W/(m·K)\n" +
                        $"Manufacturer product: {requestedValue:F4} W/(m·K)\n\n" +
                        "Do you want to replace the existing Thermal Asset?";

                    dialog.CommonButtons =
                        TaskDialogCommonButtons.Yes |
                        TaskDialogCommonButtons.No;

                    dialog.DefaultButton =
                        TaskDialogResult.No;

                    replaceThermalAsset =
                        dialog.Show() == TaskDialogResult.Yes;

                    if (!replaceThermalAsset)
                    {
                        TaskDialog.Show(
                            "SpecSynth AI",
                            "Import cancelled. Existing Thermal Asset preserved.");

                        return;
                    }
                }
            }


            // =================================================
            // 6. START IMPORT TRANSACTION GROUP
            // =================================================

            using TransactionGroup transactionGroup =
                new TransactionGroup(
                    doc,
                    "SpecSynth AI - Manufacturer Product Import");

            try
            {
                transactionGroup.Start();

                Material material;

                // A. Reuse existing material or create new.

                if (inspection.MaterialExists)
                {
                    material =
                        doc.GetElement(inspection.MaterialId)
                        as Material
                        ?? throw new InvalidOperationException(
                            "Existing material could not be retrieved.");
                }
                else
                {
                    material =
                        RevitMaterialCreationService.CreateRevitMaterial(
                            doc,
                            product);
                }


                // B. Assign Thermal Asset if required.

                bool needsThermalAsset =
                    !inspection.HasThermalAsset ||
                    replaceThermalAsset;

                if (needsThermalAsset)
                {
                    RevitMaterialCreationService.AssignThermalAsset(
                        doc,
                        material,
                        product);
                }


                // C. Assign manufacturer metadata.

                RevitMaterialCreationService.AssignManufacturerMetadata(
                    doc,
                    material,
                    product);

                // D. Populate Revit's standard Material Identity fields.
                RevitMaterialCreationService.AssignMaterialIdentityData(
                    doc,
                    material,
                    product);


                // E. Commit.

                transactionGroup.Assimilate();


                // =================================================
                // 7. SUCCESS
                // =================================================

                string materialAction =
                    inspection.MaterialExists
                        ? (alreadyLinked
                            ? "Existing material updated"
                            : "Existing material reused")
                        : "New material created";

                TaskDialog.Show(
                    "SpecSynth AI - Import Successful",

                    "Manufacturer product imported successfully.\n\n" +

                    $"Action: {materialAction}\n" +
                    $"Manufacturer: {product.Manufacturer}\n" +
                    $"Product: {product.ProductName}\n" +
                    $"Product ID: {product.ProductId}\n" +
                    $"Revit Material: {material.Name}\n" +
                    $"Thickness: {product.ThicknessMm:F1} mm\n" +
                    $"Thermal Conductivity: " +
                    $"{product.ThermalConductivityWmK:F3} W/(m·K)\n\n" +

                    "Thermal Asset, manufacturer metadata, and Revit Identity fields assigned.");
            }
            catch (Exception ex)
            {
                try
                {
                    if (transactionGroup.GetStatus() ==
                        TransactionStatus.Started)
                    {
                        transactionGroup.RollBack();
                    }
                }
                catch
                {
                    // Preserve original exception.
                }

                TaskDialog.Show(
                    "SpecSynth AI - Import Failed",

                    "Manufacturer product import failed.\n\n" +
                    "Error:\n" +
                    ex.ToString() +
                    "\n\nTransaction group rolled back.");
            }
        }


        // =====================================================
        // 8. EVENT HANDLER DESCRIPTION
        // =====================================================

        public string GetName()
        {
            return "SpecSynth AI - Manufacturer Material Import";
        }
    }
}