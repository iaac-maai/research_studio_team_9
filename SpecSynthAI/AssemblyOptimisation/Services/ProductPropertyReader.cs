using System;
using System.Text.Json;

using SpecSynthAI.AssemblyOptimisation.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

public static class ProductPropertyReader
{
    // =========================================================
    // NUMERIC PROPERTY
    // =========================================================

    public static double? GetNumericValue(
        ProductRecord product,
        string propertyName)
    {
        if (!TryGetProductProperty(
                product,
                propertyName,
                out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out double directValue) &&
            double.IsFinite(directValue))
        {
            return directValue;
        }

        if (property.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(
                property,
                "value",
                out JsonElement value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetDouble(out double numericValue) &&
            double.IsFinite(numericValue))
        {
            return numericValue;
        }

        return null;
    }

    // =========================================================
    // TEXT PROPERTY
    // =========================================================

    public static string? GetTextValue(
        ProductRecord product,
        string propertyName)
    {
        if (!TryGetProductProperty(
                product,
                propertyName,
                out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(
                property,
                "value",
                out JsonElement value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    // =========================================================
    // THERMAL CONDUCTIVITY
    // =========================================================

    public static double? GetThermalConductivityWmK(
        ProductRecord product)
    {
        double? value = GetNumericValue(
            product,
            "thermal_conductivity_W_mK");

        if (!value.HasValue ||
            !double.IsFinite(value.Value) ||
            value.Value <= 0)
        {
            return null;
        }

        return value;
    }

    // =========================================================
    // PROPERTY STATUS
    // =========================================================

    public static string? GetStatus(
        ProductRecord product,
        string propertyName)
    {
        if (!TryGetProductProperty(
                product,
                propertyName,
                out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Object ||
            !TryGetPropertyIgnoreCase(
                property,
                "status",
                out JsonElement status))
        {
            return null;
        }

        return status.ValueKind == JsonValueKind.String
            ? status.GetString()
            : null;
    }

    // =========================================================
    // PROPERTY EVIDENCE
    // =========================================================

    public static string? GetEvidenceId(
        ProductRecord product,
        string propertyName)
    {
        if (!TryGetProductProperty(
                product,
                propertyName,
                out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Object ||
            !TryGetPropertyIgnoreCase(
                property,
                "evidence_id",
                out JsonElement evidence))
        {
            return null;
        }

        return evidence.ValueKind == JsonValueKind.String
            ? evidence.GetString()
            : null;
    }

    // =========================================================
    // PRODUCT PROPERTY LOOKUP
    // =========================================================

    private static bool TryGetProductProperty(
        ProductRecord product,
        string propertyName,
        out JsonElement result)
    {
        if (product.Properties is not null)
        {
            foreach (var entry in product.Properties)
            {
                if (string.Equals(
                        entry.Key,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result = entry.Value;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    // =========================================================
    // JSON PROPERTY LOOKUP
    // =========================================================

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