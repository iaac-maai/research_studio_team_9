using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.Models
{
    public class ProductCatalog
    {
        public List<Product> Products { get; set; } = new();

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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            options.Converters.Add(
                new JsonStringEnumConverter());

            ProductCatalog catalog =
                JsonSerializer.Deserialize<ProductCatalog>(
                    json,
                    options) ?? new ProductCatalog();

            catalog.Products ??= new List<Product>();

            return catalog;
        }

        public Product? FindByRevitMaterialName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return null;
            }

            return Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(product.RevitMaterialName) &&
                string.Equals(
                    product.RevitMaterialName.Trim(),
                    materialName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public static string CheckThicknessMatch(
            Product? product,
            double installedThicknessMm,
            double toleranceMm = 0.5)
        {
            if (product == null)
            {
                return "CANNOT VERIFY";
            }

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

            bool exactMatch = product.AvailableThicknessesMm.Any(
                thickness =>
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
}