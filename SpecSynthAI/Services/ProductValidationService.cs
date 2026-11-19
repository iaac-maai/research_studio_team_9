using System;
using System.Collections.Generic;
using System.Linq;
using SpecSynthAI.Models;

namespace SpecSynthAI.Services
{
    // =========================================================
    // PRODUCT VALIDATION SERVICE
    // Validates individual products and complete catalogues.
    // Does not delete or modify product records.
    // =========================================================

    public class ProductValidationService
    {
        // =====================================================
        // 1. VALIDATE COMPLETE CATALOGUE
        // =====================================================

        public CatalogueValidationResult ValidateCatalog(
            ProductCatalog catalog)
        {
            var result = new CatalogueValidationResult();

            if (catalog == null)
            {
                result.Errors.Add("Catalogue is null.");
                return result;
            }

            if (catalog.Products == null ||
                catalog.Products.Count == 0)
            {
                result.Errors.Add("Catalogue contains no products.");
                return result;
            }

            result.TotalProducts = catalog.Products.Count;

            // Find duplicate Product IDs.
            var duplicateIds = catalog.Products
                .Where(product =>
                    product != null &&
                    !string.IsNullOrWhiteSpace(product.ProductId))
                .GroupBy(
                    product => product.ProductId.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Validate every product.
            foreach (Product product in catalog.Products)
            {
                ProductValidationResult productResult =
                    ValidateProduct(product);

                // Check duplicate IDs.
                if (!string.IsNullOrWhiteSpace(productResult.ProductId) &&
                    duplicateIds.Contains(productResult.ProductId.Trim()))
                {
                    productResult.Errors.Add(
                        $"Duplicate Product ID: {productResult.ProductId}");

                    productResult.Status = "INVALID";
                }

                result.ProductResults.Add(productResult);

                // Update catalogue statistics.
                switch (productResult.Status)
                {
                    case "READY":
                        result.ReadyProducts++;
                        break;

                    case "INCOMPLETE":
                        result.IncompleteProducts++;
                        break;

                    case "INVALID":
                        result.InvalidProducts++;
                        break;
                }

                result.Errors.AddRange(productResult.Errors);

                result.Warnings.AddRange(productResult.Warnings);
            }

            return result;
        }


        // =====================================================
        // 2. VALIDATE INDIVIDUAL PRODUCT
        // =====================================================

        public ProductValidationResult ValidateProduct(
            Product product)
        {
            var result = new ProductValidationResult
            {
                ProductId = product?.ProductId ?? "",
                ProductName = product?.ProductName ?? ""
            };

            if (product == null)
            {
                result.Errors.Add("Product record is null.");
                result.Status = "INVALID";

                return result;
            }

            // -------------------------------------------------
            // PRODUCT IDENTIFICATION
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(product.ProductId))
            {
                result.Errors.Add("Product ID is missing.");
            }

            if (string.IsNullOrWhiteSpace(product.Manufacturer))
            {
                result.Warnings.Add("Manufacturer information is missing.");
            }

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                result.Warnings.Add("Product name is missing.");
            }


            // -------------------------------------------------
            // PRODUCT THICKNESS
            // -------------------------------------------------

            if (product.ThicknessMm < 0 ||
                double.IsNaN(product.ThicknessMm) ||
                double.IsInfinity(product.ThicknessMm))
            {
                result.Errors.Add("Product thickness is invalid.");
            }
            else if (product.ThicknessMm == 0 &&
                     (product.AvailableThicknessesMm == null ||
                      product.AvailableThicknessesMm.Count == 0))
            {
                result.Warnings.Add(
                    "Product thickness information is missing.");
            }


            // -------------------------------------------------
            // AVAILABLE THICKNESSES
            // -------------------------------------------------

            if (product.AvailableThicknessesMm != null)
            {
                foreach (double thickness in product.AvailableThicknessesMm)
                {
                    if (thickness <= 0 ||
                        double.IsNaN(thickness) ||
                        double.IsInfinity(thickness))
                    {
                        result.Errors.Add(
                            "Available thickness contains an invalid value.");

                        break;
                    }
                }
            }


            // -------------------------------------------------
            // THERMAL CONDUCTIVITY
            // -------------------------------------------------

            if (product.ThermalConductivityWmK.HasValue)
            {
                double conductivity =
                    product.ThermalConductivityWmK.Value;

                if (conductivity <= 0 ||
                    double.IsNaN(conductivity) ||
                    double.IsInfinity(conductivity))
                {
                    result.Errors.Add(
                        "Thermal conductivity must be greater than zero.");
                }
            }
            else
            {
                result.Warnings.Add(
                    "Thermal conductivity is missing.");
            }


            // -------------------------------------------------
            // COMPRESSIVE STRENGTH
            // -------------------------------------------------

            if (product.CompressiveStrengthKPa < 0 ||
                double.IsNaN(product.CompressiveStrengthKPa) ||
                double.IsInfinity(product.CompressiveStrengthKPa))
            {
                result.Errors.Add(
                    "Compressive strength contains an invalid value.");
            }
            else if (product.CompressiveStrengthKPa == 0)
            {
                result.Warnings.Add(
                    "Compressive strength is missing or unverified.");
            }


            // -------------------------------------------------
            // COMPRESSIVE STRENGTH TOLERANCE
            // -------------------------------------------------

            if (product.CompressiveStrengthToleranceKPa < 0 ||
                double.IsNaN(product.CompressiveStrengthToleranceKPa) ||
                double.IsInfinity(product.CompressiveStrengthToleranceKPa))
            {
                result.Errors.Add(
                    "Compressive strength tolerance is invalid.");
            }


            // -------------------------------------------------
            // FIRE CLASSIFICATION
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(product.ReactionToFire))
            {
                result.Warnings.Add(
                    "Reaction to fire classification is missing.");
            }


            // -------------------------------------------------
            // DOCUMENTATION
            // -------------------------------------------------

            bool hasDocumentation =
                !string.IsNullOrWhiteSpace(product.SourceUrl) ||
                !string.IsNullOrWhiteSpace(product.SourceDocument) ||
                (product.Documentation != null &&
                 (!string.IsNullOrWhiteSpace(
                      product.Documentation.PrimaryProductUrl) ||
                  !string.IsNullOrWhiteSpace(
                      product.Documentation.TechnicalDatasheetUrl) ||
                  !string.IsNullOrWhiteSpace(
                      product.Documentation.DeclarationOfPerformanceUrl) ||
                  !string.IsNullOrWhiteSpace(
                      product.Documentation.EpdUrl)));

            if (!hasDocumentation)
            {
                result.Warnings.Add(
                    "Product documentation source is missing.");
            }


            // -------------------------------------------------
            // VERIFICATION STATUS
            // -------------------------------------------------

            bool hasVerificationStatus =
                !string.IsNullOrWhiteSpace(product.VerificationStatus) ||
                !string.IsNullOrWhiteSpace(
                    product.Verification?.RecordStatus);

            if (!hasVerificationStatus)
            {
                result.Warnings.Add(
                    "Product verification status is missing.");
            }


            // -------------------------------------------------
            // FINAL VALIDATION STATUS
            // -------------------------------------------------

            if (result.Errors.Count > 0)
            {
                result.Status = "INVALID";
            }
            else if (result.Warnings.Count > 0)
            {
                result.Status = "INCOMPLETE";
            }
            else
            {
                result.Status = "READY";
            }

            return result;
        }
    }


    // =========================================================
    // 3. INDIVIDUAL PRODUCT VALIDATION RESULT
    // =========================================================

    public class ProductValidationResult
    {
        public string ProductId { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string Status { get; set; } = "";

        public List<string> Errors { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }


    // =========================================================
    // 4. COMPLETE CATALOGUE VALIDATION RESULT
    // =========================================================

    public class CatalogueValidationResult
    {
        public int TotalProducts { get; set; }

        public int ReadyProducts { get; set; }

        public int IncompleteProducts { get; set; }

        public int InvalidProducts { get; set; }

        public List<ProductValidationResult> ProductResults
        {
            get;
            set;
        } = new();

        public List<string> Errors { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }
}