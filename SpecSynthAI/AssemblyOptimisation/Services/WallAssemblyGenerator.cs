using System;
using System.Collections.Generic;
using System.Linq;

using SpecSynthAI.AssemblyOptimisation.Models;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

/// <summary>
/// Generates simplified external-wall research candidates.
/// Supports product database v1.1 and v1.2.
/// </summary>
public sealed class WallAssemblyGenerator
{
    private const string SubstrateId =
        "AGTECNO3_TECNOARCILLA_24";

    private const string InteriorLiningId =
        "PLACO_BA13_1200";

    public AssemblyGenerationReport Generate(ProductDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        var report = new AssemblyGenerationReport
        {
            DatasetVersion = database.DatasetMetadata.DatasetVersion,
            ProductsRead = database.Products.Count
        };

        // -----------------------------------------------------
        // FIND FIXED PRODUCTS
        // -----------------------------------------------------

        ProductRecord? substrate = database.Products
            .FirstOrDefault(p =>
                string.Equals(
                    p.ProductId,
                    SubstrateId,
                    StringComparison.OrdinalIgnoreCase));

        ProductRecord? lining = database.Products
            .FirstOrDefault(p =>
                string.Equals(
                    p.ProductId,
                    InteriorLiningId,
                    StringComparison.OrdinalIgnoreCase));

        if (substrate is null)
        {
            report.Warnings.Add(
                $"Required substrate '{SubstrateId}' was not found.");

            return report;
        }

        if (lining is null)
        {
            report.Warnings.Add(
                $"Required interior lining '{InteriorLiningId}' was not found.");

            return report;
        }

        // -----------------------------------------------------
        // GET FIXED VARIANTS
        // -----------------------------------------------------

        ProductVariant? substrateVariant =
            GetSingleVariant(substrate);

        ProductVariant? liningVariant =
            GetSingleVariant(lining);

        if (substrateVariant is null || liningVariant is null)
        {
            report.Warnings.Add(
                "The fixed substrate or lining does not contain exactly one usable variant.");

            return report;
        }

        // -----------------------------------------------------
        // GET FIXED CONDUCTIVITY
        // -----------------------------------------------------

        double? substrateLambda =
            GetConductivity(substrate, substrateVariant);

        double? liningLambda =
            GetConductivity(lining, liningVariant);

        if (!IsPositiveFinite(substrateLambda))
        {
            report.Warnings.Add(
                $"Substrate '{SubstrateId}' has no usable thermal conductivity.");

            return report;
        }

        if (!IsPositiveFinite(liningLambda))
        {
            report.Warnings.Add(
                $"Interior lining '{InteriorLiningId}' has no usable thermal conductivity.");

            return report;
        }

        // -----------------------------------------------------
        // FIND WALL INSULATION PRODUCTS
        // -----------------------------------------------------

        List<ProductRecord> insulationProducts = database.Products
            .Where(p =>
                !string.Equals(
                    p.ProductId,
                    SubstrateId,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    p.ProductId,
                    InteriorLiningId,
                    StringComparison.OrdinalIgnoreCase) &&
                IsWallInsulationCandidate(p))
            .ToList();

        if (insulationProducts.Count == 0)
        {
            report.Warnings.Add(
                "No eligible wall-insulation products were found.");
        }

        // -----------------------------------------------------
        // GENERATE CANDIDATES
        // -----------------------------------------------------

        foreach (ProductRecord insulation in insulationProducts)
        {
            if (insulation.Variants is null ||
                insulation.Variants.Count == 0)
            {
                report.Warnings.Add(
                    $"{insulation.ProductId}: no product variants available; skipped.");

                continue;
            }

            foreach (ProductVariant variant in insulation.Variants)
            {
                if (!IsPositiveFinite(variant.ThicknessMm))
                {
                    report.Warnings.Add(
                        $"{variant.VariantId}: invalid thickness; skipped.");

                    continue;
                }

                double? insulationLambda =
                    GetConductivity(insulation, variant);

                if (!IsPositiveFinite(insulationLambda))
                {
                    report.Warnings.Add(
                        $"{variant.VariantId}: no usable conductivity; skipped.");

                    continue;
                }

                string compatibilityNote =
                    GetCompatibilityNote(insulation);

                // -------------------------------------------------
                // CREATE THERMAL LAYERS
                // -------------------------------------------------

                ThermalLayer substrateLayer =
                    ProductThermalLayerMapper.Create(
                        substrate,
                        substrateVariant,
                        isEquivalentWallResistance: true,
                        layerName: substrate.ProductName);

                ThermalLayer insulationLayer =
                    ProductThermalLayerMapper.Create(
                        insulation,
                        variant,
                        layerName:
                            $"{insulation.ProductName} ({variant.ThicknessMm:0.#}mm)");

                ThermalLayer liningLayer =
                    ProductThermalLayerMapper.Create(
                        lining,
                        liningVariant,
                        layerName: lining.ProductName);

                // -------------------------------------------------
                // CREATE WALL ASSEMBLY
                // -------------------------------------------------

                var assembly = new WallAssembly
                {
                    AssemblyName =
                        $"{substrate.ProductName} + " +
                        $"{insulation.ProductName} " +
                        $"{variant.ThicknessMm:0.#}mm + " +
                        $"{lining.ProductName}",

                    Layers = new List<ThermalLayer>
                    {
                        substrateLayer,
                        insulationLayer,
                        liningLayer
                    }
                };

                // -------------------------------------------------
                // COLLECT SOURCE IDS
                // -------------------------------------------------

                List<string> sourceIds = new();

                sourceIds.AddRange(substrateLayer.SourceIds);
                sourceIds.AddRange(insulationLayer.SourceIds);
                sourceIds.AddRange(liningLayer.SourceIds);

                sourceIds = sourceIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // -------------------------------------------------
                // CREATE CANDIDATE
                // -------------------------------------------------

                report.Candidates.Add(new AssemblyCandidate
                {
                    CandidateId =
                        $"{substrate.ProductId}__" +
                        $"{insulation.ProductId}__" +
                        $"{variant.VariantId}__" +
                        $"{lining.ProductId}",

                    AssemblyName = assembly.AssemblyName,

                    CandidateStatus =
                        "RESEARCH_CANDIDATE_NOT_SYSTEM_APPROVED",

                    CompatibilityNote = compatibilityNote,

                    SubstrateProductId = substrate.ProductId,

                    InsulationProductId = insulation.ProductId,

                    InsulationVariantId = variant.VariantId,

                    InteriorLiningProductId = lining.ProductId,

                    Assembly = assembly,

                    SourceIds = sourceIds
                });
            }
        }

        // -----------------------------------------------------
        // SUMMARY
        // -----------------------------------------------------

        report.CandidatesGenerated =
            report.Candidates.Count;

        if (report.Candidates.Count == 0)
        {
            report.Warnings.Add(
                "No assembly candidates could be generated from the available product data.");
        }

        report.Warnings.Add(
            "Candidates are simplified 1D thermal research assemblies, not approved construction systems. " +
            "Check manufacturer system compatibility, fixings, adhesives, membranes, fire strategy, " +
            "moisture behaviour, structural design and local regulations before design use.");

        return report;
    }

    // =========================================================
    // INSULATION IDENTIFICATION
    // =========================================================

    private static bool IsWallInsulationCandidate(
        ProductRecord product)
    {
        string classification =
            $"{product.Category ?? ""} {product.MaterialFamily ?? ""}"
            .ToLowerInvariant();

        bool isInsulation =
            classification.Contains("insulation") ||
            classification.Contains("insulant") ||
            classification.Contains("mineral wool") ||
            classification.Contains("glass wool") ||
            classification.Contains("stone wool") ||
            classification.Contains("polyisocyanurate") ||
            classification.Contains("polyurethane") ||
            classification.Contains("polystyrene") ||
            classification.Contains("phenolic");

        if (!isInsulation)
            return false;

        string applications = string.Join(
            " ",
            product.ApplicationsDeclared ?? new List<string>());

        applications = applications.ToLowerInvariant();

        return
            applications.Contains("wall") ||
            applications.Contains("facade") ||
            applications.Contains("façade") ||
            applications.Contains("etics") ||
            applications.Contains("external wall") ||
            applications.Contains("cavity wall") ||
            applications.Contains("ventilated facade") ||
            applications.Contains("ventilated façade");
    }

    // =========================================================
    // CONDUCTIVITY SELECTION
    // =========================================================

    private static double? GetConductivity(
        ProductRecord product,
        ProductVariant variant)
    {
        if (IsPositiveFinite(variant.ThermalConductivityWmK))
            return variant.ThermalConductivityWmK;

        double? productLambda =
            ProductPropertyReader.GetThermalConductivityWmK(product);

        return IsPositiveFinite(productLambda)
            ? productLambda
            : null;
    }

    // =========================================================
    // VARIANT HELPERS
    // =========================================================

    private static ProductVariant? GetSingleVariant(
        ProductRecord product)
    {
        if (product.Variants is null ||
            product.Variants.Count != 1)
        {
            return null;
        }

        ProductVariant variant = product.Variants[0];

        return IsPositiveFinite(variant.ThicknessMm)
            ? variant
            : null;
    }

    // =========================================================
    // NUMERIC VALIDATION
    // =========================================================

    private static bool IsPositiveFinite(double? value)
    {
        return value.HasValue &&
               double.IsFinite(value.Value) &&
               value.Value > 0;
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) &&
               value > 0;
    }

    // =========================================================
    // COMPATIBILITY NOTES
    // =========================================================

    private static string GetCompatibilityNote(
        ProductRecord insulation)
    {
        if (string.Equals(
                insulation.ProductId,
                "SIKA_SIKATHERM_MW_040_F_MD",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Manufacturer describes this as an ETICS component; compatibility " +
                "with the selected substrate and complete system is not confirmed.";
        }

        if (string.Equals(
                insulation.ProductId,
                "BAUMIT_PROTHERM_EPS",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Manufacturer declares facade ETICS application; compatibility with " +
                "the selected substrate and other system components must be confirmed.";
        }

        return
            "Manufacturer declares a wall-related insulation application; confirm suitability " +
            "for this specific wall build-up and complete construction system.";
    }
}