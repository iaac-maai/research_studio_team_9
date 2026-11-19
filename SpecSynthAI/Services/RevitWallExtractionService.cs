using Autodesk.Revit.DB;

using SpecSynthAI.Models;

using ThermalEngine.Models;

using System;
using System.Collections.Generic;

namespace SpecSynthAI.Services
{
    public class RevitWallExtractionService
    {
        /// <summary>
        /// Extracts all construction layers from a Revit wall
        /// and converts them into a ThermalEngine WallAssembly.
        ///
        /// Product catalogue is optional.
        /// When unavailable, Revit thermal material assets
        /// are used where available.
        /// </summary>
        public WallAssembly ExtractAssembly(
            Document document,
            Wall wall,
            ProductCatalog? catalog)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(wall);

            // STEP 1: Retrieve wall type.
            WallType wallType = wall.WallType;

            // STEP 2: Retrieve compound structure.
            CompoundStructure? structure =
                wallType.GetCompoundStructure();

            if (structure == null)
            {
                throw new InvalidOperationException(
                    $"Wall type '{wallType.Name}' has no compound structure.");
            }

            // STEP 3: Retrieve all construction layers.
            IList<CompoundStructureLayer> revitLayers =
                structure.GetLayers();

            var thermalLayers = new List<ThermalLayer>();

            // STEP 4: Process every layer.
            for (int index = 0; index < revitLayers.Count; index++)
            {
                CompoundStructureLayer revitLayer =
                    revitLayers[index];

                Material? material =
                    document.GetElement(revitLayer.MaterialId) as Material;

                string materialName =
                    material?.Name ??
                    $"Unassigned Material - Layer {index + 1}";

                double thicknessMm =
                    UnitUtils.ConvertFromInternalUnits(
                        revitLayer.Width,
                        UnitTypeId.Millimeters);

                double? conductivity = null;

                ConductivitySource conductivitySource =
                    ConductivitySource.Unknown;

                // STEP 5: Try verified manufacturer product.
                Product? product =
                    material == null || catalog == null
                        ? null
                        : catalog.FindByRevitMaterialName(materialName);

                if (product != null &&
                    string.Equals(
                        product.VerificationStatus,
                        "verified",
                        StringComparison.OrdinalIgnoreCase) &&
                    product.ThermalConductivityWmK.HasValue &&
                    double.IsFinite(
                        product.ThermalConductivityWmK.Value) &&
                    product.ThermalConductivityWmK.Value > 0)
                {
                    conductivity =
                        product.ThermalConductivityWmK.Value;

                    conductivitySource =
                        ConductivitySource.VerifiedManufacturerProduct;
                }

                // STEP 6: Fall back to Revit thermal asset.
                if (!conductivity.HasValue && material != null)
                {
                    double? revitConductivity =
                        GetRevitThermalConductivity(
                            document,
                            material);

                    if (revitConductivity.HasValue)
                    {
                        conductivity = revitConductivity.Value;

                        conductivitySource =
                            ConductivitySource.RevitThermalAsset;
                    }
                }

                // STEP 7: Create ThermalEngine layer.
                thermalLayers.Add(new ThermalLayer
                {
                    LayerName = materialName,
                    ThicknessMm = thicknessMm,
                    ThermalConductivityWmK = conductivity,
                    ConductivitySource = conductivitySource
                });
            }

            // STEP 8: Return complete assembly.
            return new WallAssembly
            {
                AssemblyName =
                    $"{wallType.Name} - Revit Wall ID {wall.Id.Value}",

                Layers = thermalLayers
            };
        }

        /// <summary>
        /// Retrieves thermal conductivity from a Revit
        /// material thermal asset.
        /// </summary>
        private static double? GetRevitThermalConductivity(
            Document document,
            Material material)
        {
            ElementId thermalAssetId =
                material.ThermalAssetId;

            if (thermalAssetId == ElementId.InvalidElementId)
                return null;

            PropertySetElement? thermalPropertySet =
                document.GetElement(thermalAssetId)
                as PropertySetElement;

            if (thermalPropertySet == null)
                return null;

            using ThermalAsset thermalAsset =
                thermalPropertySet.GetThermalAsset();

            if (!thermalAsset.IsValidObject)
                return null;

            if (thermalAsset.ThermalMaterialType !=
                ThermalMaterialType.Solid)
            {
                return null;
            }

            double conductivityInternal =
                thermalAsset.ThermalConductivity;

            if (!double.IsFinite(conductivityInternal) ||
                conductivityInternal <= 0)
            {
                return null;
            }

            double conductivityWmK =
                UnitUtils.ConvertFromInternalUnits(
                    conductivityInternal,
                    UnitTypeId.WattsPerMeterKelvin);

            if (!double.IsFinite(conductivityWmK) ||
                conductivityWmK <= 0)
            {
                return null;
            }

            return conductivityWmK;
        }
    }
}