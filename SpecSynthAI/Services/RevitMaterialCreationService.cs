using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

using SpecSynthAI.Models;

namespace SpecSynthAI.Services
{
    public static class RevitMaterialCreationService
    {
        // =========================================================
        // 1. METADATA SCHEMA
        // =========================================================

        // New schema version.
        // ThicknessMm is now stored as a proper Length field.

        private static readonly Guid MetadataSchemaGuid =
            new Guid("B72A8D61-91F2-4C31-A829-5F6D9C21A703");


        private static Schema? GetMetadataSchema()
        {
            return Schema.Lookup(MetadataSchemaGuid);
        }


        private static Schema GetOrCreateMetadataSchema()
        {
            Schema? existingSchema = GetMetadataSchema();

            if (existingSchema != null)
                return existingSchema;

            SchemaBuilder builder =
                new SchemaBuilder(MetadataSchemaGuid);

            builder.SetSchemaName(
                "SpecSynthProductMetadata");

            builder.SetDocumentation(
                "Manufacturer and product information managed by SpecSynth AI.");

            builder.AddSimpleField(
                "ProductId",
                typeof(string));

            builder.AddSimpleField(
                "Manufacturer",
                typeof(string));

            builder.AddSimpleField(
                "ProductName",
                typeof(string));

            builder.AddSimpleField(
                "Category",
                typeof(string));

            // Thickness is a physical length.
            // Store and retrieve explicitly in millimetres.

            FieldBuilder thicknessField =
                builder.AddSimpleField(
                    "ThicknessMm",
                    typeof(double));

            thicknessField.SetSpec(
                SpecTypeId.Length);

            builder.AddSimpleField(
                "VerificationStatus",
                typeof(string));

            builder.AddSimpleField(
                "SourceDocument",
                typeof(string));

            builder.AddSimpleField(
                "SourceUrl",
                typeof(string));

            return builder.Finish();
        }


        // =========================================================
        // 2. PRODUCT VALIDATION
        // =========================================================

        public static MaterialImportValidationResult ValidateProduct(
            Product? product)
        {
            var result =
                new MaterialImportValidationResult();

            if (product == null)
            {
                result.Errors.Add(
                    "No product was selected.");

                return result;
            }

            if (string.IsNullOrWhiteSpace(product.ProductId))
                result.Errors.Add("Product ID is missing.");

            if (string.IsNullOrWhiteSpace(product.Manufacturer))
                result.Errors.Add("Manufacturer is missing.");

            if (string.IsNullOrWhiteSpace(product.ProductName))
                result.Errors.Add("Product name is missing.");

            if (string.IsNullOrWhiteSpace(product.RevitMaterialName))
                result.Errors.Add("Revit material name is missing.");

            if (string.IsNullOrWhiteSpace(product.Category))
                result.Errors.Add("Product category is missing.");

            if (product.ThicknessMm <= 0 ||
                double.IsNaN(product.ThicknessMm) ||
                double.IsInfinity(product.ThicknessMm))
            {
                result.Errors.Add(
                    "Product thickness must be positive and finite.");
            }

            if (!product.ThermalConductivityWmK.HasValue)
            {
                result.Errors.Add(
                    "Thermal conductivity is missing.");
            }
            else
            {
                double conductivity =
                    product.ThermalConductivityWmK.Value;

                if (double.IsNaN(conductivity) ||
                    double.IsInfinity(conductivity) ||
                    conductivity <= 0)
                {
                    result.Errors.Add(
                        "Thermal conductivity must be positive and finite.");
                }
            }

            if (!string.Equals(
                product.VerificationStatus?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(
                    "Product is not verified. Import is blocked.");
            }

            result.IsValid =
                result.Errors.Count == 0;

            return result;
        }


        // =========================================================
        // 3. DOCUMENT AND MATERIAL VALIDATION
        // =========================================================

        private static void ValidateDocumentMaterial(
            Document doc,
            Material material)
        {
            if (doc == null ||
                material == null ||
                doc.IsReadOnly)
            {
                throw new InvalidOperationException(
                    "Invalid Revit document or material.");
            }

            if (!material.IsValidObject)
            {
                throw new InvalidOperationException(
                    "The Revit material is no longer valid.");
            }

            Element? existingElement =
                doc.GetElement(material.Id);

            if (existingElement == null ||
                existingElement.UniqueId != material.UniqueId)
            {
                throw new InvalidOperationException(
                    "Material does not belong to the active Revit document.");
            }
        }


        // =========================================================
        // 4. INSPECT EXISTING MATERIAL
        // =========================================================

        public static MaterialInspectionResult InspectExistingMaterial(
            Document doc,
            Product product)
        {
            var result =
                new MaterialInspectionResult();

            if (doc == null)
            {
                result.Errors.Add(
                    "Revit document is unavailable.");

                return result;
            }

            if (product == null ||
                string.IsNullOrWhiteSpace(product.RevitMaterialName))
            {
                result.Errors.Add(
                    "Product or Revit material name is missing.");

                return result;
            }

            // Find material.

            Material? material =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .FirstOrDefault(m =>
                        string.Equals(
                            m.Name,
                            product.RevitMaterialName,
                            StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                result.MaterialExists = false;
                result.Status = "MATERIAL_NOT_FOUND";

                return result;
            }

            result.MaterialExists = true;
            result.MaterialName = material.Name;
            result.MaterialId = material.Id;


            // Inspect Thermal Asset.

            ElementId thermalAssetId =
                material.ThermalAssetId;

            if (thermalAssetId == ElementId.InvalidElementId)
            {
                result.HasThermalAsset = false;
            }
            else
            {
                PropertySetElement? propertySet =
                    doc.GetElement(thermalAssetId)
                    as PropertySetElement;

                if (propertySet == null)
                {
                    result.Errors.Add(
                        "Thermal Asset could not be retrieved.");

                    return result;
                }

                using ThermalAsset thermalAsset =
                    propertySet.GetThermalAsset();

                if (thermalAsset.ThermalMaterialType !=
                    ThermalMaterialType.Solid)
                {
                    result.Errors.Add(
                        "Existing Thermal Asset is not a solid material asset.");

                    return result;
                }

                result.HasThermalAsset = true;

                result.ThermalConductivityWmK =
                    UnitUtils.ConvertFromInternalUnits(
                        thermalAsset.ThermalConductivity,
                        UnitTypeId.WattsPerMeterKelvin);
            }


            // Inspect SpecSynth metadata.

            Schema? schema =
                GetMetadataSchema();

            if (schema == null)
            {
                result.Status = "MATERIAL_FOUND";
                return result;
            }

            Entity entity =
                material.GetEntity(schema);

            if (!entity.IsValid())
            {
                result.Status = "MATERIAL_FOUND";
                return result;
            }

            result.HasManufacturerMetadata = true;

            result.ExistingProductId =
                entity.Get<string>(
                    schema.GetField("ProductId"));

            result.ExistingManufacturer =
                entity.Get<string>(
                    schema.GetField("Manufacturer"));

            result.ExistingProductName =
                entity.Get<string>(
                    schema.GetField("ProductName"));

            result.IsLinkedToSelectedProduct =
                string.Equals(
                    result.ExistingProductId,
                    product.ProductId,
                    StringComparison.OrdinalIgnoreCase);


            // Final status.

            if (result.IsLinkedToSelectedProduct)
            {
                result.Status = "PRODUCT_ALREADY_LINKED";
            }
            else if (result.HasThermalAsset)
            {
                result.Status = "MATERIAL_HAS_THERMAL_ASSET";
            }
            else
            {
                result.Status = "MATERIAL_WITHOUT_THERMAL_ASSET";
            }

            return result;
        }


        // =========================================================
        // 5. DUPLICATE DETECTION
        // =========================================================

        public static bool ProductAlreadyImported(
            Document doc,
            Product product)
        {
            if (doc == null || product == null)
                return false;

            Schema? schema =
                GetMetadataSchema();

            if (schema == null)
                return false;

            var materials =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>();

            foreach (Material material in materials)
            {
                Entity entity =
                    material.GetEntity(schema);

                if (!entity.IsValid())
                    continue;

                string existingProductId =
                    entity.Get<string>(
                        schema.GetField("ProductId"));

                if (string.Equals(
                    existingProductId,
                    product.ProductId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        public static bool MaterialAlreadyExists(
            Document doc,
            string materialName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .Any(m =>
                    string.Equals(
                        m.Name,
                        materialName,
                        StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // 6. CREATE REVIT MATERIAL
        // =========================================================

        public static Material CreateRevitMaterial(
            Document doc,
            Product product)
        {
            MaterialImportValidationResult validation =
                ValidateProduct(product);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            if (doc == null || doc.IsReadOnly)
            {
                throw new InvalidOperationException(
                    "Revit document is unavailable or read-only.");
            }

            if (MaterialAlreadyExists(
                doc,
                product.RevitMaterialName))
            {
                throw new InvalidOperationException(
                    $"Material '{product.RevitMaterialName}' already exists.");
            }

            ElementId materialId;

            using Transaction transaction =
                new Transaction(
                    doc,
                    "SpecSynth AI - Create Material");

            transaction.Start();

            try
            {
                materialId =
                    Material.Create(
                        doc,
                        product.RevitMaterialName);

                transaction.Commit();
            }
            catch
            {
                if (transaction.GetStatus() ==
                    TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                throw;
            }

            return doc.GetElement(materialId) as Material
                ?? throw new InvalidOperationException(
                    "Material creation failed.");
        }


        // =========================================================
        // 7. CREATE AND ASSIGN THERMAL ASSET
        // =========================================================

        public static void AssignThermalAsset(
            Document doc,
            Material material,
            Product product)
        {
            MaterialImportValidationResult validation =
                ValidateProduct(product);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            ValidateDocumentMaterial(doc, material);

            // Convert manufacturer conductivity into
            // Revit internal units.

            double conductivityInternal =
                UnitUtils.ConvertToInternalUnits(
                    product.ThermalConductivityWmK!.Value,
                    UnitTypeId.WattsPerMeterKelvin);

            using var thermalAsset =
                new ThermalAsset(
                    product.RevitMaterialName + "_Thermal",
                    ThermalMaterialType.Solid);

            thermalAsset.Behavior =
                StructuralBehavior.Isotropic;

            thermalAsset.ThermalConductivity =
                conductivityInternal;


            using Transaction transaction =
                new Transaction(
                    doc,
                    "SpecSynth AI - Assign Thermal Asset");

            transaction.Start();

            try
            {
                PropertySetElement propertySet =
                    PropertySetElement.Create(
                        doc,
                        thermalAsset);

                // Revit 2027 material aspect assignment.

                material.SetMaterialAspectByPropertySet(
                    MaterialAspect.Thermal,
                    propertySet.Id);

                transaction.Commit();
            }
            catch
            {
                if (transaction.GetStatus() ==
                    TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                throw;
            }
        }


        // =========================================================
        // 8. ASSIGN STANDARD REVIT MATERIAL IDENTITY DATA
        // =========================================================

        public static void AssignMaterialIdentityData(
            Document doc,
            Material material,
            Product product)
        {
            MaterialImportValidationResult validation =
                ValidateProduct(product);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            ValidateDocumentMaterial(doc, material);

            using Transaction transaction =
                new Transaction(
                    doc,
                    "SpecSynth AI - Assign Material Identity Data");

            transaction.Start();

            try
            {
                SetMaterialStringParameter(
                    material,
                    BuiltInParameter.ALL_MODEL_MANUFACTURER,
                    product.Manufacturer,
                    "Manufacturer");

                SetMaterialStringParameter(
                    material,
                    BuiltInParameter.ALL_MODEL_MODEL,
                    product.ProductName,
                    "Model");

                SetMaterialStringParameter(
                    material,
                    BuiltInParameter.ALL_MODEL_DESCRIPTION,
                    product.Category,
                    "Description");

                SetMaterialStringParameter(
                    material,
                    BuiltInParameter.ALL_MODEL_URL,
                    product.SourceUrl,
                    "URL");

                SetMaterialStringParameter(
                    material,
                    BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                    "SpecSynth Product ID: " + product.ProductId,
                    "Comments");

                transaction.Commit();
            }
            catch
            {
                if (transaction.GetStatus() ==
                    TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                throw;
            }
        }

        private static void SetMaterialStringParameter(
            Material material,
            BuiltInParameter parameterId,
            string? value,
            string displayName)
        {
            Parameter? parameter =
                material.get_Parameter(parameterId);

            if (parameter == null)
            {
                throw new InvalidOperationException(
                    $"Revit material parameter '{displayName}' was not found.");
            }

            if (parameter.IsReadOnly)
            {
                throw new InvalidOperationException(
                    $"Revit material parameter '{displayName}' is read-only.");
            }

            parameter.Set(value ?? "");
        }


        // =========================================================
        // 9. ASSIGN MANUFACTURER METADATA
        // =========================================================

        public static void AssignManufacturerMetadata(
            Document doc,
            Material material,
            Product product)
        {
            MaterialImportValidationResult validation =
                ValidateProduct(product);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            ValidateDocumentMaterial(doc, material);

            Schema schema =
                GetOrCreateMetadataSchema();

            Entity entity =
                new Entity(schema);

            entity.Set(
                schema.GetField("ProductId"),
                product.ProductId);

            entity.Set(
                schema.GetField("Manufacturer"),
                product.Manufacturer);

            entity.Set(
                schema.GetField("ProductName"),
                product.ProductName);

            entity.Set(
                schema.GetField("Category"),
                product.Category);

            // Explicitly write thickness in millimetres.
            // Field spec is Length.

            entity.Set<double>(
                schema.GetField("ThicknessMm"),
                product.ThicknessMm,
                UnitTypeId.Millimeters);

            entity.Set(
                schema.GetField("VerificationStatus"),
                product.VerificationStatus ?? "");

            entity.Set(
                schema.GetField("SourceDocument"),
                product.SourceDocument ?? "");

            entity.Set(
                schema.GetField("SourceUrl"),
                product.SourceUrl ?? "");


            using Transaction transaction =
                new Transaction(
                    doc,
                    "SpecSynth AI - Assign Manufacturer Metadata");

            transaction.Start();

            try
            {
                material.SetEntity(entity);

                transaction.Commit();
            }
            catch
            {
                if (transaction.GetStatus() ==
                    TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                throw;
            }
        }


        // =========================================================
        // 10. READ MANUFACTURER METADATA
        // =========================================================

        public static Entity? GetManufacturerMetadata(
            Material material)
        {
            Schema? schema =
                GetMetadataSchema();

            if (schema == null)
                return null;

            Entity entity =
                material.GetEntity(schema);

            return entity.IsValid()
                ? entity
                : null;
        }
    }


    // =============================================================
    // 11. MATERIAL INSPECTION RESULT
    // =============================================================

    public class MaterialInspectionResult
    {
        public bool MaterialExists { get; set; }

        public string MaterialName { get; set; } = "";

        public ElementId MaterialId { get; set; } =
            ElementId.InvalidElementId;

        public bool HasThermalAsset { get; set; }

        public double? ThermalConductivityWmK { get; set; }

        public bool HasManufacturerMetadata { get; set; }

        public string ExistingProductId { get; set; } = "";

        public string ExistingManufacturer { get; set; } = "";

        public string ExistingProductName { get; set; } = "";

        public bool IsLinkedToSelectedProduct { get; set; }

        public string Status { get; set; } = "";

        public List<string> Errors { get; set; } = new();
    }


    // =============================================================
    // 12. VALIDATION RESULT
    // =============================================================

    public class MaterialImportValidationResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}