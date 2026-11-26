using ThermalEngine.Configuration;
using ThermalEngine.Models;

namespace ThermalEngine.Services;

public class ThermalCalculator
{
    // STEP 1: Calculate resistance of one material layer.

    public double CalculateLayerResistance(ThermalLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (!double.IsFinite(layer.ThicknessMm) ||
            layer.ThicknessMm <= 0)
        {
            throw new ArgumentException(
                "Thickness must be positive and finite.");
        }

        double? conductivity = layer.ThermalConductivityWmK;

        if (!conductivity.HasValue ||
            !double.IsFinite(conductivity.Value) ||
            conductivity.Value <= 0)
        {
            throw new ArgumentException(
                "Valid thermal conductivity is required.");
        }

        // Convert millimetres into metres.
        double thicknessMetres = layer.ThicknessMm / 1000.0;

        // R = thickness / thermal conductivity.
        return thicknessMetres / conductivity.Value;
    }


    // STEP 2: Calculate resistance of all material layers.

    public double CalculateTotalResistance(
        IEnumerable<ThermalLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        var layerList = layers.ToList();

        if (layerList.Count == 0)
        {
            throw new ArgumentException(
                "Wall must contain at least one layer.");
        }

        double totalResistance = 0;

        foreach (var layer in layerList)
        {
            totalResistance += CalculateLayerResistance(layer);
        }

        return totalResistance;
    }


    // STEP 3: Calculate layer-only U-value.

    public double CalculateUValue(IEnumerable<ThermalLayer> layers)
    {
        double totalResistance =
            CalculateTotalResistance(layers);

        return 1.0 / totalResistance;
    }


    // STEP 4: Calculate U-value including surface resistances.

    public double CalculateUValue(
        IEnumerable<ThermalLayer> layers,
        HeatFlowDirection direction)
    {
        double layerResistance =
            CalculateTotalResistance(layers);

        SurfaceResistanceValues surfaceValues =
            SurfaceResistanceDefaults.Get(direction);

        double totalResistance =
            surfaceValues.InternalResistance +
            layerResistance +
            surfaceValues.ExternalResistance;

        return 1.0 / totalResistance;
    }


    // STEP 5: Calculate assembly using horizontal heat flow.
    // Default direction is for an external wall.

    public ThermalCalculationResult CalculateAssembly(
        IEnumerable<ThermalLayer> layers)
    {
        return CalculateAssembly(
            layers,
            HeatFlowDirection.Horizontal);
    }


    // STEP 6: Calculate assembly using selected heat-flow direction.

    public ThermalCalculationResult CalculateAssembly(
        IEnumerable<ThermalLayer> layers,
        HeatFlowDirection direction)
    {
        var result = new ThermalCalculationResult();

        if (layers == null)
        {
            result.Errors.Add("Wall assembly is missing.");
            return result;
        }

        var layerList = layers.ToList();

        if (layerList.Count == 0)
        {
            result.Errors.Add(
                "Wall assembly contains no layers.");

            return result;
        }


        // STEP 6A: Validate all material layers.

        foreach (var layer in layerList)
        {
            if (layer == null)
            {
                result.Errors.Add(
                    "A material layer is missing.");

                continue;
            }

            if (string.IsNullOrWhiteSpace(layer.LayerName))
            {
                result.Errors.Add(
                    "A layer has no material name.");
            }

            if (!double.IsFinite(layer.ThicknessMm) ||
                layer.ThicknessMm <= 0)
            {
                result.Errors.Add(
                    $"{layer.LayerName}: Invalid thickness.");
            }

            double? conductivity =
                layer.ThermalConductivityWmK;

            if (!conductivity.HasValue ||
                !double.IsFinite(conductivity.Value) ||
                conductivity.Value <= 0)
            {
                result.Errors.Add(
                    $"{layer.LayerName}: Missing or invalid conductivity.");
            }
        }


        // STEP 6B: Stop if validation errors exist.

        if (result.Errors.Count > 0)
        {
            result.IsValid = false;
            return result;
        }


        // STEP 6C: Retrieve surface resistance defaults.

        SurfaceResistanceValues surfaceValues;

        try
        {
            surfaceValues =
                SurfaceResistanceDefaults.Get(direction);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            result.Errors.Add(ex.Message);
            return result;
        }


        // STEP 6D: Calculate material-layer resistance.

        double layerResistance =
            CalculateTotalResistance(layerList);


        // STEP 6E: Calculate complete assembly resistance.

        double totalResistance =
            surfaceValues.InternalResistance +
            layerResistance +
            surfaceValues.ExternalResistance;

        if (!double.IsFinite(totalResistance) ||
            totalResistance <= 0)
        {
            result.Errors.Add(
                "Calculated total resistance is invalid.");

            return result;
        }


        // STEP 6F: Calculate U-value.

        double uValue = 1.0 / totalResistance;

        if (!double.IsFinite(uValue) || uValue <= 0)
        {
            result.Errors.Add(
                "Calculated U-value is invalid.");

            return result;
        }


        // STEP 6G: Calculate individual material-layer results
        // and transfer product provenance.

        foreach (var layer in layerList)
        {
            double individualResistance =
                CalculateLayerResistance(layer);

            // Contribution relative to complete assembly resistance.

            double contribution =
                (individualResistance / totalResistance) * 100.0;


            // STEP 6G-1: Validate declared versus calculated R-value.

            double? discrepancyPercentage =
                CalculateResistanceDiscrepancy(
                    layer,
                    individualResistance);


            result.LayerResults.Add(new ThermalLayerResult
            {
                // Basic calculation results

                LayerName = layer.LayerName,

                ThicknessMm = layer.ThicknessMm,

                ThermalConductivityWmK =
                    layer.ThermalConductivityWmK!.Value,

                ThermalResistance = individualResistance,

                ResistanceContributionPercentage = contribution,


                // Product identification

                ProductId = layer.ProductId,

                VariantId = layer.VariantId,

                ProductCode = layer.ProductCode,


                // Conductivity provenance

                ConductivitySource =
                    layer.ConductivitySource.ToString(),

                ConductivityStatus =
                    layer.ConductivityStatus,

                ConductivityEvidenceId =
                    layer.ConductivityEvidenceId,


                // Source evidence

                SourceIds = new List<string>(layer.SourceIds),

                ResistanceEvidence = layer.ResistanceEvidence,

                ResistanceScope = layer.ResistanceScope.ToString(),


                // Manufacturer-declared resistance

                ManufacturerDeclaredResistance =
                    layer.ManufacturerDeclaredResistance,

                ResistanceStatus = layer.ResistanceStatus,


                // Selected resistance

                SelectedResistance = individualResistance,

                ResistanceSelectionMethod =
                    "CalculatedFromThicknessAndConductivity",


                // Discrepancy validation

                DiscrepancyPercentage =
                    discrepancyPercentage
            });
        }


        // STEP 6H: Store all calculation results.

        result.IsValid = true;

        result.LayerResistance = layerResistance;

        result.InternalSurfaceResistance =
            surfaceValues.InternalResistance;

        result.ExternalSurfaceResistance =
            surfaceValues.ExternalResistance;

        result.TotalResistance = totalResistance;

        result.UValue = uValue;

        return result;
    }


    // STEP 7: Compare manufacturer-declared and calculated R-values.
    //
    // Comparison is performed only when both values represent
    // the same product-variant scope.
    //
    // Equivalent wall/assembly values are deliberately excluded.

    private static double? CalculateResistanceDiscrepancy(
        ThermalLayer layer,
        double calculatedResistance)
    {
        if (layer.ResistanceScope !=
            ResistanceValueScope.ProductVariant)
        {
            return null;
        }

        double? declaredResistance =
            layer.ManufacturerDeclaredResistance;

        if (!declaredResistance.HasValue ||
            !double.IsFinite(declaredResistance.Value) ||
            declaredResistance.Value <= 0)
        {
            return null;
        }

        if (!double.IsFinite(calculatedResistance) ||
            calculatedResistance <= 0)
        {
            return null;
        }

        double discrepancy =
            Math.Abs(
                calculatedResistance - declaredResistance.Value)
            / declaredResistance.Value
            * 100.0;

        return double.IsFinite(discrepancy)
            ? discrepancy
            : null;
    }
}