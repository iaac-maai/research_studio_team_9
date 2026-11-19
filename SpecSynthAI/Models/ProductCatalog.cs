using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.Models
{
    // =========================================================
    // PRODUCT CATALOG
    // Supports legacy and unified JSON datasets.
    // =========================================================

    public class ProductCatalog
    {
        // -----------------------------------------------------
        // PRODUCT RECORDS
        // -----------------------------------------------------

        public List<Product> Products { get; set; } = new();

        // -----------------------------------------------------
        // DATASET METADATA
        // -----------------------------------------------------

        [JsonPropertyName("dataset_metadata")]
        public DatasetMetadata? Metadata { get; set; }

        // -----------------------------------------------------
        // JSON OPTIONS
        // -----------------------------------------------------

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            options.Converters.Add(
                new JsonStringEnumConverter());

            return options;
        }

        // -----------------------------------------------------
        // LOAD CATALOGUE
        // -----------------------------------------------------

        public static ProductCatalog Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Catalogue file path cannot be empty.",
                    nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Product catalogue was not found.",
                    filePath);
            }

            string json = File.ReadAllText(filePath);

            ProductCatalog catalog =
                JsonSerializer.Deserialize<ProductCatalog>(
                    json,
                    CreateJsonOptions()) ?? new ProductCatalog();

            catalog.Products ??= new List<Product>();

            foreach (Product product in catalog.Products)
            {
                NormaliseProduct(product);
            }

            return catalog;
        }

        // -----------------------------------------------------
        // NORMALISE PRODUCT
        // Maps unified properties into legacy properties.
        // -----------------------------------------------------

        private static void NormaliseProduct(Product product)
        {
            if (product == null)
                return;

            // IDENTIFICATION

            if (string.IsNullOrWhiteSpace(product.ProductId))
            {
                product.ProductId =
                    product.Identification?.ProductId ?? "";
            }

            if (string.IsNullOrWhiteSpace(product.Manufacturer))
            {
                product.Manufacturer =
                    product.Identification?.Manufacturer ?? "";
            }

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                product.ProductName =
                    product.Identification?.ProductName ?? "";
            }

            // CLASSIFICATION

            if (string.IsNullOrWhiteSpace(product.Category))
            {
                product.Category =
                    product.Classification?.Category ?? "";
            }

            if (string.IsNullOrWhiteSpace(product.MaterialFamily))
            {
                product.MaterialFamily =
                    product.Classification?.MaterialFamily ?? "";
            }

            // BIM MAPPING

            if (string.IsNullOrWhiteSpace(product.RevitMaterialName))
            {
                product.RevitMaterialName =
                    product.BimMapping?.RevitMaterialName ?? "";
            }

            // DOCUMENTATION

            if (string.IsNullOrWhiteSpace(product.SourceUrl))
            {
                product.SourceUrl =
                    product.Documentation?.PrimaryProductUrl ?? "";
            }

            if (string.IsNullOrWhiteSpace(product.SourceDocument))
            {
                product.SourceDocument =
                    product.Documentation?.TechnicalDatasheetUrl ?? "";
            }

            // VERIFICATION

            if (string.IsNullOrWhiteSpace(product.VerificationStatus))
            {
                product.VerificationStatus =
                    product.Verification?.RecordStatus ?? "";
            }

            // PRODUCT PROPERTIES

            if (product.ProductProperties != null)
            {
                var physical = product.ProductProperties.Physical;
                var thermal = product.ProductProperties.Thermal;
                var mechanical = product.ProductProperties.Mechanical;
                var fire = product.ProductProperties.Fire;

                if (product.ThicknessMm <= 0)
                {
                    product.ThicknessMm =
                        GetDouble(physical, "thickness_mm") ?? 0;
                }

                if (product.ThermalConductivityWmK == null)
                {
                    product.ThermalConductivityWmK =
                        GetDouble(
                            thermal,
                            "thermal_conductivity_W_mK");
                }

                if (product.CompressiveStrengthKPa <= 0)
                {
                    product.CompressiveStrengthKPa =
                        GetDouble(
                            mechanical,
                            "compressive_strength_kPa") ?? 0;
                }

                if (product.CompressiveStrengthToleranceKPa <= 0)
                {
                    product.CompressiveStrengthToleranceKPa =
                        GetDouble(
                            mechanical,
                            "compressive_strength_tolerance_kPa") ?? 0;
                }

                if (string.IsNullOrWhiteSpace(product.ReactionToFire))
                {
                    product.ReactionToFire =
                        GetString(fire, "reaction_to_fire") ?? "";
                }

                if (product.AvailableThicknessesMm.Count == 0)
                {
                    product.AvailableThicknessesMm =
                        GetDoubleList(
                            physical,
                            "available_thicknesses_mm");
                }
            }

            // VARIANT THICKNESS FALLBACK

            if (product.AvailableThicknessesMm.Count == 0 &&
                product.AvailableVariants != null)
            {
                product.AvailableThicknessesMm =
                    product.AvailableVariants
                        .Where(v => v.ThicknessMm.HasValue)
                        .Select(v => v.ThicknessMm!.Value)
                        .Where(v =>
                            v > 0 &&
                            !double.IsNaN(v) &&
                            !double.IsInfinity(v))
                        .Distinct()
                        .OrderBy(v => v)
                        .ToList();
            }

            // SINGLE AVAILABLE THICKNESS

            if (product.ThicknessMm <= 0 &&
                product.AvailableThicknessesMm.Count == 1)
            {
                product.ThicknessMm =
                    product.AvailableThicknessesMm[0];
            }
        }

        // -----------------------------------------------------
        // GET NUMERIC PROPERTY
        // -----------------------------------------------------

        private static double? GetDouble(
            Dictionary<string, ProductPropertyValue> properties,
            string key)
        {
            if (properties == null ||
                !properties.TryGetValue(key, out var property) ||
                property?.Value == null)
            {
                return null;
            }

            try
            {
                if (property.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Number &&
                        element.TryGetDouble(out double number))
                    {
                        return number;
                    }

                    if (element.ValueKind == JsonValueKind.String &&
                        double.TryParse(
                            element.GetString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out number))
                    {
                        return number;
                    }
                }

                return Convert.ToDouble(
                    property.Value,
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------
        // GET STRING PROPERTY
        // -----------------------------------------------------

        private static string? GetString(
            Dictionary<string, ProductPropertyValue> properties,
            string key)
        {
            if (properties == null ||
                !properties.TryGetValue(key, out var property) ||
                property?.Value == null)
            {
                return null;
            }

            if (property.Value is JsonElement element)
            {
                return element.ValueKind == JsonValueKind.String
                    ? element.GetString()
                    : element.ToString();
            }

            return property.Value.ToString();
        }

        // -----------------------------------------------------
        // GET NUMERIC LIST
        // -----------------------------------------------------

        private static List<double> GetDoubleList(
            Dictionary<string, ProductPropertyValue> properties,
            string key)
        {
            var result = new List<double>();

            if (properties == null ||
                !properties.TryGetValue(key, out var property) ||
                property?.Value == null)
            {
                return result;
            }

            try
            {
                if (property.Value is JsonElement element &&
                    element.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Number &&
                            item.TryGetDouble(out double value) &&
                            value > 0 &&
                            !double.IsNaN(value) &&
                            !double.IsInfinity(value))
                        {
                            result.Add(value);
                        }
                    }
                }
                else if (property.Value is IEnumerable<double> values)
                {
                    result.AddRange(
                        values.Where(v =>
                            v > 0 &&
                            !double.IsNaN(v) &&
                            !double.IsInfinity(v)));
                }
            }
            catch
            {
                // Invalid values remain unpopulated.
            }

            return result
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        // -----------------------------------------------------
        // FIND PRODUCT BY ID
        // -----------------------------------------------------

        public Product? FindByProductId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return null;

            return Products.FirstOrDefault(product =>
                string.Equals(
                    product.ProductId?.Trim(),
                    productId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        // -----------------------------------------------------
        // FIND PRODUCT BY REVIT MATERIAL NAME
        // -----------------------------------------------------

        public Product? FindByRevitMaterialName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return null;

            return Products.FirstOrDefault(product =>
                string.Equals(
                    product.RevitMaterialName?.Trim(),
                    materialName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        // -----------------------------------------------------
        // CHECK THICKNESS MATCH
        // -----------------------------------------------------

        public static string CheckThicknessMatch(
            Product? product,
            double installedThicknessMm,
            double toleranceMm = 0.5)
        {
            if (product == null)
                return "CANNOT VERIFY";

            if (double.IsNaN(installedThicknessMm) ||
                double.IsInfinity(installedThicknessMm) ||
                installedThicknessMm <= 0)
            {
                return "CANNOT VERIFY";
            }

            if (double.IsNaN(toleranceMm) ||
                double.IsInfinity(toleranceMm) ||
                toleranceMm < 0)
            {
                return "CANNOT VERIFY";
            }

            if (product.AvailableThicknessesMm == null ||
                product.AvailableThicknessesMm.Count == 0)
            {
                return "CANNOT VERIFY";
            }

            bool exactMatch =
                product.AvailableThicknessesMm.Any(thickness =>
                    thickness > 0 &&
                    !double.IsNaN(thickness) &&
                    !double.IsInfinity(thickness) &&
                    Math.Abs(thickness - installedThicknessMm)
                        <= toleranceMm);

            return exactMatch
                ? "EXACT MATCH"
                : "NO EXACT MATCH";
        }
    }

    // =========================================================
    // DATASET METADATA
    // =========================================================

    public class DatasetMetadata
    {
        [JsonPropertyName("dataset_name")]
        public string DatasetName { get; set; } = "";

        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; } = "";

        [JsonPropertyName("dataset_version")]
        public string DatasetVersion { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("last_updated")]
        public string LastUpdated { get; set; } = "";

        [JsonPropertyName("unit_system")]
        public string UnitSystem { get; set; } = "";

        [JsonPropertyName("default_currency")]
        public string DefaultCurrency { get; set; } = "";

        public List<string> Notes { get; set; } = new();
    }
}