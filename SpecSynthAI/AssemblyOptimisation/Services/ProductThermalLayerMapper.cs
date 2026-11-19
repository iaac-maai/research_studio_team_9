using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using ThermalEngine.Models;
using SpecSynthAI.AssemblyOptimisation.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

public static class ProductThermalLayerMapper
{
    // =========================================================
    // 1. JSON ELEMENT API
    // =========================================================

    public static ThermalLayer Create(
        JsonElement product,
        JsonElement variant,
        bool isEquivalentWallResistance = false)
    {
        string productId =
            GetString(product, "product_id") ?? "";

        string productName =
            GetString(product, "product_name") ?? productId;

        string variantId =
            GetString(variant, "variant_id") ?? "";

        double thickness =
            GetRequiredNumber(variant, "thickness_mm");

        JsonElement productProperties =
            GetProperty(product, "properties");

        double? variantLambda =
            GetValueNumber(
                GetProperty(variant, "thermal_conductivity_W_mK"));

        double? productLambda =
            GetValueNumber(
                GetProperty(
                    productProperties,
                    "thermal_conductivity_W_mK"));

        double? lambda =
            IsPositiveFinite(variantLambda)
                ? variantLambda
                : productLambda;

        double? variantR =
            GetValueNumber(
                GetProperty(
                    variant,
                    "thermal_resistance_m2K_W"));

        string? rStatus =
            GetString(variant, "r_status");

        List<string> evidenceIds = new();

        // Product-level conductivity evidence.
        string? conductivityEvidence =
            GetNestedString(
                productProperties,
                "thermal_conductivity_W_mK",
                "evidence_id");

        if (!string.IsNullOrWhiteSpace(conductivityEvidence))
            evidenceIds.Add(conductivityEvidence);

        // Product-level source IDs.
        AddJsonSourceIds(
            GetProperty(product, "source_ids"),
            evidenceIds);

        // Variant-level source IDs.
        AddJsonSourceIds(
            GetProperty(variant, "source_ids"),
            evidenceIds);

        // Variant-level conductivity evidence.
        string? variantEvidence =
            GetString(variant, "conductivity_evidence_id");

        if (!string.IsNullOrWhiteSpace(variantEvidence))
            evidenceIds.Add(variantEvidence);

        evidenceIds = CleanEvidenceIds(evidenceIds);

        return CreateLayer(
            productId,
            productName,
            variantId,
            GetString(variant, "product_code"),
            thickness,
            lambda,
            variantR,
            rStatus,
            evidenceIds,
            IsManufacturerDeclared(rStatus),
            isEquivalentWallResistance);
    }


    // =========================================================
    // 2. TYPED DTO API
    // =========================================================

    public static ThermalLayer Create(
        ProductRecord product,
        ProductVariant variant,
        bool isEquivalentWallResistance = false,
        string? layerName = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(variant);

        if (string.IsNullOrWhiteSpace(product.ProductId))
        {
            throw new ArgumentException(
                "Product is missing product_id.",
                nameof(product));
        }

        if (string.IsNullOrWhiteSpace(variant.VariantId))
        {
            throw new ArgumentException(
                "Variant is missing variant_id.",
                nameof(variant));
        }

        // -----------------------------------------------------
        // 2.1 VALIDATE THICKNESS
        // -----------------------------------------------------

        if (!variant.ThicknessMm.HasValue ||
            !double.IsFinite(variant.ThicknessMm.Value) ||
            variant.ThicknessMm.Value <= 0)
        {
            throw new ArgumentException(
                $"Variant '{variant.VariantId}' has invalid thickness.",
                nameof(variant));
        }

        // Extract the validated nullable value.
        double thickness = variant.ThicknessMm.Value;


        // -----------------------------------------------------
        // 2.2 SELECT CONDUCTIVITY
        // -----------------------------------------------------

        // Priority 1: variant-specific conductivity.
        double? lambda =
            IsPositiveFinite(variant.ThermalConductivityWmK)
                ? variant.ThermalConductivityWmK
                : null;

        // Priority 2: parent product conductivity.
        if (!lambda.HasValue)
        {
            lambda =
                ProductPropertyReader.GetThermalConductivityWmK(
                    product);
        }


        // -----------------------------------------------------
        // 2.3 COLLECT PRODUCT-LEVEL EVIDENCE
        // -----------------------------------------------------

        List<string> evidenceIds = new();

        if (product.SourceIds is not null)
        {
            evidenceIds.AddRange(product.SourceIds);
        }

        string? productEvidence =
            ProductPropertyReader.GetEvidenceId(
                product,
                "thermal_conductivity_W_mK");

        if (!string.IsNullOrWhiteSpace(productEvidence))
        {
            evidenceIds.Add(productEvidence);
        }


        // -----------------------------------------------------
        // 2.4 COLLECT VARIANT-LEVEL EVIDENCE
        // -----------------------------------------------------

        if (variant.AdditionalData is not null)
        {
            foreach (var item in variant.AdditionalData)
            {
                // Variant source_ids.
                if (string.Equals(
                        item.Key,
                        "source_ids",
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Value.ValueKind == JsonValueKind.Array)
                {
                    AddJsonSourceIds(
                        item.Value,
                        evidenceIds);
                }

                // Variant conductivity evidence ID.
                if (string.Equals(
                        item.Key,
                        "conductivity_evidence_id",
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Value.ValueKind == JsonValueKind.String)
                {
                    string? id = item.Value.GetString();

                    if (!string.IsNullOrWhiteSpace(id))
                        evidenceIds.Add(id);
                }

                // Alternative evidence_id field.
                if (string.Equals(
                        item.Key,
                        "evidence_id",
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Value.ValueKind == JsonValueKind.String)
                {
                    string? id = item.Value.GetString();

                    if (!string.IsNullOrWhiteSpace(id))
                        evidenceIds.Add(id);
                }

                // Evidence nested inside variant conductivity.
                if (string.Equals(
                        item.Key,
                        "thermal_conductivity_W_mK",
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Value.ValueKind == JsonValueKind.Object)
                {
                    string? id =
                        GetString(item.Value, "evidence_id");

                    if (!string.IsNullOrWhiteSpace(id))
                        evidenceIds.Add(id);
                }
            }
        }

        evidenceIds = CleanEvidenceIds(evidenceIds);


        // -----------------------------------------------------
        // 2.5 CREATE THERMAL LAYER
        // -----------------------------------------------------

        return CreateLayer(
            product.ProductId,
            layerName ?? product.ProductName,
            variant.VariantId,
            variant.ProductCode,
            thickness,
            lambda,
            variant.ThermalResistanceM2K_W,
            variant.RStatus,
            evidenceIds,
            IsManufacturerDeclared(variant.RStatus),
            isEquivalentWallResistance);
    }


    // =========================================================
    // 3. THERMAL LAYER CONSTRUCTION
    // =========================================================

    private static ThermalLayer CreateLayer(
        string productId,
        string productName,
        string variantId,
        string? productCode,
        double thickness,
        double? lambda,
        double? variantR,
        string? rStatus,
        IEnumerable<string>? sourceIds,
        bool declaredStatus,
        bool isEquivalentWallResistance)
    {
        List<string> evidenceIds =
            CleanEvidenceIds(
                sourceIds ?? Enumerable.Empty<string>());

        bool validLambda =
            IsPositiveFinite(lambda);

        bool validResistance =
            declaredStatus &&
            IsPositiveFinite(variantR);

        return new ThermalLayer
        {
            // BASIC INFORMATION

            LayerName =
                string.IsNullOrWhiteSpace(productName)
                    ? productId
                    : productName,

            ProductId = productId,

            VariantId = variantId,

            ProductCode = productCode,

            ThicknessMm = thickness,

            // THERMAL CONDUCTIVITY

            ThermalConductivityWmK =
                validLambda ? lambda : null,

            ConductivitySource =
                validLambda
                    ? ConductivitySource.VerifiedManufacturerProduct
                    : ConductivitySource.Unknown,

            // SOURCE EVIDENCE

            SourceIds = evidenceIds,

            ResistanceEvidence =
                string.Join("; ", evidenceIds),

            // DECLARED RESISTANCE

            ManufacturerDeclaredResistance =
                validResistance ? variantR : null,

            ResistanceStatus = rStatus,

            ResistanceScope =
                validResistance
                    ? (isEquivalentWallResistance
                        ? ResistanceValueScope.EquivalentWallOrAssembly
                        : ResistanceValueScope.ProductVariant)
                    : ResistanceValueScope.Unknown,

            // CALCULATION SELECTION

            SelectedResistance = null,

            ResistanceSelectionMethod =
                "CalculatedFromThicknessAndConductivity"
        };
    }


    // =========================================================
    // 4. EVIDENCE HELPERS
    // =========================================================

    private static void AddJsonSourceIds(
        JsonElement sources,
        List<string> evidenceIds)
    {
        if (sources.ValueKind != JsonValueKind.Array)
            return;

        foreach (JsonElement source in sources.EnumerateArray())
        {
            if (source.ValueKind == JsonValueKind.String)
            {
                string? id = source.GetString();

                if (!string.IsNullOrWhiteSpace(id))
                    evidenceIds.Add(id);
            }
        }
    }


    private static List<string> CleanEvidenceIds(
        IEnumerable<string> sourceIds)
    {
        return sourceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    // =========================================================
    // 5. STATUS HELPERS
    // =========================================================

    private static bool IsManufacturerDeclared(string? status)
    {
        return
            string.Equals(
                status,
                "manufacturer_declared",
                StringComparison.OrdinalIgnoreCase)

            ||

            string.Equals(
                status,
                "manufacturer_declared_wall_value",
                StringComparison.OrdinalIgnoreCase)

            ||

            string.Equals(
                status,
                "certified_declared",
                StringComparison.OrdinalIgnoreCase);
    }


    // =========================================================
    // 6. NUMERIC VALIDATION
    // =========================================================

    private static bool IsPositiveFinite(double? value)
    {
        return value.HasValue &&
               double.IsFinite(value.Value) &&
               value.Value > 0;
    }


    // =========================================================
    // 7. JSON HELPERS
    // =========================================================

    private static JsonElement GetProperty(
        JsonElement parent,
        string name)
    {
        if (parent.ValueKind != JsonValueKind.Object)
            return default;

        foreach (JsonProperty property in parent.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return default;
    }


    private static string? GetString(
        JsonElement parent,
        string name)
    {
        JsonElement value = GetProperty(parent, name);

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }


    private static string? GetNestedString(
        JsonElement parent,
        string objectName,
        string propertyName)
    {
        JsonElement nested =
            GetProperty(parent, objectName);

        return GetString(nested, propertyName);
    }


    private static double GetRequiredNumber(
        JsonElement parent,
        string name)
    {
        double? value =
            GetValueNumber(GetProperty(parent, name));

        if (!IsPositiveFinite(value))
        {
            throw new ArgumentException(
                $"Required positive numeric JSON property '{name}' is missing or invalid.");
        }

        return value.Value;
    }


    private static double? GetValueNumber(
        JsonElement property)
    {
        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out double direct))
        {
            return double.IsFinite(direct)
                ? direct
                : null;
        }

        if (property.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(
                property,
                "value",
                out JsonElement value))
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDouble(out double number))
            {
                return double.IsFinite(number)
                    ? number
                    : null;
            }
        }

        return null;
    }


    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string name,
        out JsonElement result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        name,
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
}