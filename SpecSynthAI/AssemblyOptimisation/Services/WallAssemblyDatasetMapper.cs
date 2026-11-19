using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using SpecSynthAI.AssemblyOptimisation.Models;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

public sealed class WallAssemblyDatasetMapper
{
    private const string SupportedSchemaVersion = "1.1.0";
    private const double ThicknessToleranceMm = 0.01;

    // =========================================================
    // LOAD AND MAP
    // =========================================================

    public AssemblyDatasetMappingReport LoadAndMap(
        string assemblyDatasetPath,
        ProductDatabase productDatabase)
    {
        if (string.IsNullOrWhiteSpace(assemblyDatasetPath))
        {
            throw new ArgumentException(
                "Assembly dataset path cannot be empty.",
                nameof(assemblyDatasetPath));
        }

        if (!File.Exists(assemblyDatasetPath))
        {
            throw new FileNotFoundException(
                "Assembly dataset JSON was not found.",
                assemblyDatasetPath);
        }

        ArgumentNullException.ThrowIfNull(productDatabase);

        using JsonDocument document =
            JsonDocument.Parse(File.ReadAllText(assemblyDatasetPath));

        return Map(document.RootElement, productDatabase);
    }

    // =========================================================
    // MAP DATASET
    // =========================================================

    public AssemblyDatasetMappingReport Map(
        JsonElement datasetRoot,
        ProductDatabase productDatabase)
    {
        ArgumentNullException.ThrowIfNull(productDatabase);

        if (datasetRoot.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "Assembly dataset root must be a JSON object.");
        }

        string schemaVersion =
            ReadString(datasetRoot, "schema_version") ?? "";

        if (!string.Equals(
                schemaVersion,
                SupportedSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported assembly schema version '{schemaVersion}'. " +
                $"Expected '{SupportedSchemaVersion}'.");
        }

        if (!TryGetProperty(
                datasetRoot,
                "assemblies",
                out JsonElement assembliesElement) ||
            assembliesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "Assembly dataset must contain an assemblies array.");
        }

        var report = new AssemblyDatasetMappingReport
        {
            DatasetId = ReadString(datasetRoot, "dataset_id") ?? "",
            SchemaVersion = schemaVersion
        };

        Dictionary<string, ProductRecord> products =
            productDatabase.Products
                .Where(product =>
                    !string.IsNullOrWhiteSpace(product.ProductId))
                .ToDictionary(
                    product => product.ProductId,
                    product => product,
                    StringComparer.OrdinalIgnoreCase);

        HashSet<string> seenAssemblyNames =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement assemblyElement
                 in assembliesElement.EnumerateArray())
        {
            AssemblyDatasetMappingItem item =
                MapAssembly(assemblyElement, products);

            if (!seenAssemblyNames.Add(item.AssemblyName))
            {
                item.Errors.Add(
                    $"Assembly name '{item.AssemblyName}' is duplicated.");

                item.IsCalculationReady = false;
                item.Assembly = null;
            }

            report.AssemblyResults.Add(item);

            if (item.IsCalculationReady &&
                item.Assembly is not null)
            {
                report.CalculationReadyAssemblies.Add(item.Assembly);
            }
        }

        report.TotalAssemblies = report.AssemblyResults.Count;

        report.CalculationReadyCount =
            report.CalculationReadyAssemblies.Count;

        report.NotReadyCount =
            report.TotalAssemblies - report.CalculationReadyCount;

        return report;
    }

    // =========================================================
    // MAP INDIVIDUAL ASSEMBLY
    // =========================================================

    private static AssemblyDatasetMappingItem MapAssembly(
        JsonElement assemblyElement,
        IReadOnlyDictionary<string, ProductRecord> products)
    {
        string assemblyId =
            ReadString(assemblyElement, "assembly_id") ?? "";

        string assemblyName =
            ReadString(assemblyElement, "assembly_name") ?? assemblyId;

        var item = new AssemblyDatasetMappingItem
        {
            AssemblyId = assemblyId,
            AssemblyName = assemblyName
        };

        if (string.IsNullOrWhiteSpace(assemblyId))
        {
            item.Errors.Add("Assembly is missing assembly_id.");
        }

        if (!TryGetProperty(
                assemblyElement,
                "layers",
                out JsonElement layersElement) ||
            layersElement.ValueKind != JsonValueKind.Array ||
            layersElement.GetArrayLength() == 0)
        {
            item.Errors.Add("Assembly contains no layers.");
            return item;
        }

        var orderedLayers = layersElement
            .EnumerateArray()
            .Select(layer => new
            {
                Element = layer,
                Order = ReadInt(layer, "layer_order")
            })
            .OrderBy(layer => layer.Order)
            .ToList();

        if (orderedLayers.Any(layer => layer.Order <= 0))
        {
            item.Errors.Add(
                "Every layer must have a positive layer_order.");
        }

        if (orderedLayers
            .Select(layer => layer.Order)
            .Distinct()
            .Count() != orderedLayers.Count)
        {
            item.Errors.Add(
                "Layer order values must be unique within an assembly.");
        }

        List<ThermalLayer> thermalLayers = new();

        foreach (var orderedLayer in orderedLayers)
        {
            JsonElement layer = orderedLayer.Element;

            string layerType =
                ReadString(layer, "layer_type") ?? "";

            string function =
                ReadString(layer, "function") ?? "other";

            double? assemblyThickness =
                ReadNumber(layer, "thickness_mm");

            if (!IsPositiveFinite(assemblyThickness))
            {
                item.Errors.Add(
                    $"Layer {orderedLayer.Order} ({function}) " +
                    "has invalid thickness_mm.");

                continue;
            }

            double thickness = assemblyThickness.Value;

            switch (layerType.ToLowerInvariant())
            {
                case "product":

                    MapProductLayer(
                        layer,
                        orderedLayer.Order,
                        thickness,
                        products,
                        thermalLayers,
                        item);

                    break;

                case "generic_material":

                    MapGenericMaterialLayer(
                        layer,
                        orderedLayer.Order,
                        thickness,
                        thermalLayers,
                        item);

                    break;

                case "void":

                    item.Errors.Add(
                        $"Layer {orderedLayer.Order} is a void/cavity. " +
                        "The current 1D ThermalCalculator does not model " +
                        "air-space resistance.");

                    break;

                case "system":

                    item.Errors.Add(
                        $"Layer {orderedLayer.Order} is a system layer. " +
                        "It needs a validated system-level thermal model.");

                    break;

                default:

                    item.Errors.Add(
                        $"Layer {orderedLayer.Order} has unsupported " +
                        $"layer_type '{layerType}'.");

                    break;
            }
        }

        if (thermalLayers.Count != orderedLayers.Count &&
            item.Errors.Count == 0)
        {
            item.Errors.Add(
                "Not every assembly layer was mapped to a thermal layer.");
        }

        if (item.Errors.Count == 0)
        {
            item.Assembly = new WallAssembly
            {
                AssemblyName = assemblyName,
                Layers = thermalLayers
            };

            item.IsCalculationReady = true;
        }

        return item;
    }

    // =========================================================
    // PRODUCT LAYER
    // =========================================================

    private static void MapProductLayer(
        JsonElement layer,
        int layerOrder,
        double assemblyThickness,
        IReadOnlyDictionary<string, ProductRecord> products,
        ICollection<ThermalLayer> thermalLayers,
        AssemblyDatasetMappingItem item)
    {
        string? productId =
            ReadString(layer, "product_id");

        string? variantId =
            ReadString(layer, "variant_id");

        if (string.IsNullOrWhiteSpace(productId) ||
            string.IsNullOrWhiteSpace(variantId))
        {
            item.Errors.Add(
                $"Product layer {layerOrder} is missing product_id or variant_id.");

            return;
        }

        if (!products.TryGetValue(
                productId,
                out ProductRecord? product))
        {
            item.Errors.Add(
                $"Layer {layerOrder}: product '{productId}' was not found.");

            return;
        }

        ProductVariant? variant =
            product.Variants.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.VariantId,
                    variantId,
                    StringComparison.OrdinalIgnoreCase));

        if (variant is null)
        {
            item.Errors.Add(
                $"Layer {layerOrder}: variant '{variantId}' was not found.");

            return;
        }

        if (!variant.ThicknessMm.HasValue ||
            !double.IsFinite(variant.ThicknessMm.Value) ||
            variant.ThicknessMm.Value <= 0)
        {
            item.Errors.Add(
                $"Layer {layerOrder}: catalogue variant '{variantId}' " +
                "has missing or invalid thickness.");

            return;
        }

        double catalogueThickness = variant.ThicknessMm.Value;

        if (Math.Abs(catalogueThickness - assemblyThickness) >
            ThicknessToleranceMm)
        {
            item.Errors.Add(
                $"Layer {layerOrder}: assembly thickness " +
                $"({assemblyThickness:0.###} mm) does not match " +
                $"catalogue variant '{variantId}' thickness " +
                $"({catalogueThickness:0.###} mm).");

            return;
        }

        ThermalLayer thermalLayer;

        try
        {
            thermalLayer =
                ProductThermalLayerMapper.Create(product, variant);
        }
        catch (ArgumentException ex)
        {
            item.Errors.Add(
                $"Layer {layerOrder}: {ex.Message}");

            return;
        }

        if (!IsPositiveFinite(
                thermalLayer.ThermalConductivityWmK))
        {
            item.Errors.Add(
                $"Layer {layerOrder} ({product.ProductName}): " +
                "catalogue conductivity is missing or invalid.");

            return;
        }

        thermalLayers.Add(thermalLayer);
    }

    // =========================================================
    // GENERIC MATERIAL LAYER
    // =========================================================

    private static void MapGenericMaterialLayer(
        JsonElement layer,
        int layerOrder,
        double thickness,
        ICollection<ThermalLayer> thermalLayers,
        AssemblyDatasetMappingItem item)
    {
        string? materialName =
            ReadString(layer, "material_name");

        double? conductivity =
            ReadNumber(layer, "thermal_conductivity_W_mK");

        if (string.IsNullOrWhiteSpace(materialName))
        {
            item.Errors.Add(
                $"Generic material layer {layerOrder} is missing material_name.");

            return;
        }

        if (!IsPositiveFinite(conductivity))
        {
            item.Errors.Add(
                $"Generic material layer {layerOrder} ({materialName}) " +
                "has no valid thermal conductivity.");

            return;
        }

        thermalLayers.Add(new ThermalLayer
        {
            LayerName = materialName,

            ThicknessMm = thickness,

            ThermalConductivityWmK = conductivity,

            ConductivitySource = ConductivitySource.Unknown,

            ResistanceScope = ResistanceValueScope.Unknown,

            ResistanceEvidence = ReadString(layer, "notes"),

            ResistanceSelectionMethod =
                "CalculatedFromThicknessAndConductivity"
        });

        item.Warnings.Add(
            $"Generic material layer {layerOrder} uses assembly-supplied " +
            "conductivity; verify and attach source evidence.");
    }

    // =========================================================
    // JSON HELPERS
    // =========================================================

    private static string? ReadString(
        JsonElement element,
        string propertyName)
    {
        if (!TryGetProperty(
                element,
                propertyName,
                out JsonElement value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static int ReadInt(
        JsonElement element,
        string propertyName)
    {
        return TryGetProperty(
                   element,
                   propertyName,
                   out JsonElement value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt32(out int result)
            ? result
            : 0;
    }

    private static double? ReadNumber(
        JsonElement element,
        string propertyName)
    {
        if (!TryGetProperty(
                element,
                propertyName,
                out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out double result) ||
            !double.IsFinite(result))
        {
            return null;
        }

        return result;
    }

    private static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result = property.Value;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool IsPositiveFinite(double? value)
    {
        return value.HasValue &&
               double.IsFinite(value.Value) &&
               value.Value > 0;
    }
}

// =============================================================
// MAPPING REPORT
// =============================================================

public sealed class AssemblyDatasetMappingReport
{
    public string DatasetId { get; set; } = "";

    public string SchemaVersion { get; set; } = "";

    public int TotalAssemblies { get; set; }

    public int CalculationReadyCount { get; set; }

    public int NotReadyCount { get; set; }

    public List<AssemblyDatasetMappingItem> AssemblyResults
    {
        get;
        set;
    } = new();

    public List<WallAssembly> CalculationReadyAssemblies
    {
        get;
        set;
    } = new();
}

// =============================================================
// INDIVIDUAL ASSEMBLY RESULT
// =============================================================

public sealed class AssemblyDatasetMappingItem
{
    public string AssemblyId { get; set; } = "";

    public string AssemblyName { get; set; } = "";

    public bool IsCalculationReady { get; set; }

    public WallAssembly? Assembly { get; set; }

    public List<string> Errors { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
}