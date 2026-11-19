using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using SpecSynthAI.AssemblyOptimisation.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

public sealed class ProductDatabaseReader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // =========================================================
    // V1.1 SINGLE-FILE LOADER
    // =========================================================

    public ProductDatabase Load(string filePath)
    {
        ValidateFilePath(filePath);

        string json = File.ReadAllText(filePath);

        ProductDatabase? database =
            JsonSerializer.Deserialize<ProductDatabase>(
                json,
                _jsonOptions);

        if (database is null)
        {
            throw new InvalidDataException(
                "The product database JSON could not be read.");
        }

        Validate(database);

        return database;
    }

    // =========================================================
    // V1.2 THREE-FILE LOADER
    // =========================================================

    public ProductDatabase Load(
        string productsFilePath,
        string variantsFilePath,
        string sourcesFilePath)
    {
        ValidateFilePath(productsFilePath);
        ValidateFilePath(variantsFilePath);
        ValidateFilePath(sourcesFilePath);

        using JsonDocument productsDocument =
            JsonDocument.Parse(File.ReadAllText(productsFilePath));

        using JsonDocument variantsDocument =
            JsonDocument.Parse(File.ReadAllText(variantsFilePath));

        using JsonDocument sourcesDocument =
            JsonDocument.Parse(File.ReadAllText(sourcesFilePath));

        JsonElement productsRoot = productsDocument.RootElement;
        JsonElement variantsRoot = variantsDocument.RootElement;
        JsonElement sourcesRoot = sourcesDocument.RootElement;

        ProductDatabaseMetadata metadata =
            ReadMetadata(productsRoot);

        List<ProductRecord> products =
            ReadArray<ProductRecord>(productsRoot, "products");

        List<ProductVariant> variants =
            ReadArray<ProductVariant>(variantsRoot, "variants");

        List<JsonElement> sources =
            ReadRawArray(sourcesRoot, "sources");

        ValidateDatasetVersions(
            productsRoot,
            variantsRoot,
            sourcesRoot);

        AttachVariants(products, variants);

        ProductDatabase database = new()
        {
            DatasetMetadata = metadata,
            Products = products,
            Sources = sources
        };

        Validate(database);

        return database;
    }

    // =========================================================
    // METADATA
    // =========================================================

    private ProductDatabaseMetadata ReadMetadata(JsonElement root)
    {
        if (!TryGetProperty(
                root,
                "dataset_metadata",
                out JsonElement metadataElement))
        {
            throw new InvalidDataException(
                "Products file is missing dataset_metadata.");
        }

        return JsonSerializer.Deserialize<ProductDatabaseMetadata>(
                   metadataElement.GetRawText(),
                   _jsonOptions)
               ?? throw new InvalidDataException(
                   "Could not read dataset metadata.");
    }

    // =========================================================
    // ARRAY DESERIALIZATION
    // =========================================================

    private List<T> ReadArray<T>(
        JsonElement root,
        string arrayName)
    {
        JsonElement array = GetArrayElement(root, arrayName);

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Expected array '{arrayName}'.");
        }

        List<T> result = new();

        foreach (JsonElement element in array.EnumerateArray())
        {
            T? item = JsonSerializer.Deserialize<T>(
                element.GetRawText(),
                _jsonOptions);

            if (item is null)
            {
                throw new InvalidDataException(
                    $"Could not deserialize an item in '{arrayName}'.");
            }

            result.Add(item);
        }

        return result;
    }

    private static List<JsonElement> ReadRawArray(
        JsonElement root,
        string arrayName)
    {
        JsonElement array = GetArrayElement(root, arrayName);

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Expected array '{arrayName}'.");
        }

        return array
            .EnumerateArray()
            .Select(element => element.Clone())
            .ToList();
    }

    private static JsonElement GetArrayElement(
        JsonElement root,
        string arrayName)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            TryGetProperty(root, arrayName, out JsonElement array))
        {
            return array;
        }

        throw new InvalidDataException(
            $"Could not find array '{arrayName}'.");
    }

    // =========================================================
    // VARIANT JOIN
    // =========================================================

    private static void AttachVariants(
        List<ProductRecord> products,
        List<ProductVariant> variants)
    {
        Dictionary<string, ProductRecord> productLookup =
            products.ToDictionary(
                product => product.ProductId,
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> allVariantIds =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (ProductVariant variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant.VariantId))
            {
                throw new InvalidDataException(
                    "A variant is missing variant_id.");
            }

            if (!allVariantIds.Add(variant.VariantId))
            {
                throw new InvalidDataException(
                    $"Duplicate variant ID: {variant.VariantId}");
            }

            if (string.IsNullOrWhiteSpace(variant.ProductId))
            {
                throw new InvalidDataException(
                    $"Variant '{variant.VariantId}' is missing product_id.");
            }

            if (!productLookup.TryGetValue(
                    variant.ProductId,
                    out ProductRecord? product))
            {
                throw new InvalidDataException(
                    $"Variant '{variant.VariantId}' references unknown " +
                    $"product '{variant.ProductId}'.");
            }

            // Missing thickness is allowed in the database.
            // Invalid supplied thickness is rejected.
            if (variant.ThicknessMm.HasValue &&
                (!double.IsFinite(variant.ThicknessMm.Value) ||
                 variant.ThicknessMm.Value <= 0))
            {
                throw new InvalidDataException(
                    $"Variant '{variant.VariantId}' has invalid thickness.");
            }

            if (variant.ThermalConductivityWmK.HasValue &&
                (!double.IsFinite(variant.ThermalConductivityWmK.Value) ||
                 variant.ThermalConductivityWmK.Value <= 0))
            {
                throw new InvalidDataException(
                    $"Variant '{variant.VariantId}' has invalid conductivity.");
            }

            if (variant.ThermalResistanceM2K_W.HasValue &&
                (!double.IsFinite(variant.ThermalResistanceM2K_W.Value) ||
                 variant.ThermalResistanceM2K_W.Value <= 0))
            {
                throw new InvalidDataException(
                    $"Variant '{variant.VariantId}' has invalid thermal resistance.");
            }

            product.Variants.Add(variant);
        }
    }

    // =========================================================
    // DATASET VERSION CHECK
    // =========================================================

    private static void ValidateDatasetVersions(
        JsonElement productsRoot,
        JsonElement variantsRoot,
        JsonElement sourcesRoot)
    {
        string? productsVersion = GetDatasetVersion(productsRoot);
        string? variantsVersion = GetDatasetVersion(variantsRoot);
        string? sourcesVersion = GetDatasetVersion(sourcesRoot);

        if (!string.Equals(
                productsVersion,
                variantsVersion,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                productsVersion,
                sourcesVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Dataset version mismatch between products, variants and sources.");
        }
    }

    private static string? GetDatasetVersion(JsonElement root)
    {
        if (!TryGetProperty(
                root,
                "dataset_metadata",
                out JsonElement metadata))
        {
            return null;
        }

        if (!TryGetProperty(
                metadata,
                "dataset_version",
                out JsonElement version))
        {
            return null;
        }

        return version.ValueKind == JsonValueKind.String
            ? version.GetString()
            : null;
    }

    // =========================================================
    // DATABASE VALIDATION
    // =========================================================

    private static void Validate(ProductDatabase database)
    {
        if (database.Products is null ||
            database.Products.Count == 0)
        {
            throw new InvalidDataException(
                "The product database contains no products.");
        }

        var duplicateProductIds = database.Products
            .GroupBy(
                product => product.ProductId,
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateProductIds.Count > 0)
        {
            throw new InvalidDataException(
                "Duplicate product IDs found: " +
                string.Join(", ", duplicateProductIds));
        }

        foreach (ProductRecord product in database.Products)
        {
            if (string.IsNullOrWhiteSpace(product.ProductId))
            {
                throw new InvalidDataException(
                    "A product is missing product_id.");
            }

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                throw new InvalidDataException(
                    $"Product '{product.ProductId}' is missing product_name.");
            }

            product.Variants ??= new();

            foreach (ProductVariant variant in product.Variants)
            {
                if (string.IsNullOrWhiteSpace(variant.VariantId))
                {
                    throw new InvalidDataException(
                        $"A variant in '{product.ProductId}' is missing variant_id.");
                }

                // Null is allowed; invalid supplied values are not.
                if (variant.ThicknessMm.HasValue &&
                    (!double.IsFinite(variant.ThicknessMm.Value) ||
                     variant.ThicknessMm.Value <= 0))
                {
                    throw new InvalidDataException(
                        $"Variant '{variant.VariantId}' has invalid thickness.");
                }
            }
        }
    }

    // =========================================================
    // JSON PROPERTY HELPER
    // =========================================================

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

    // =========================================================
    // FILE VALIDATION
    // =========================================================

    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Dataset file not found.",
                filePath);
        }
    }
}