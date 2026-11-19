using System.Globalization;
using System.IO;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective;

/// <summary>
/// Maps synthetic multi-objective CSV rows into the unified contract.
/// Synthetic provenance and restrictions are retained.
/// </summary>
public sealed class SyntheticScenarioCsvMapper
{
    private const string DatasetVersion = "synthetic_multiobjective_v1.0";

    public List<UnifiedAssemblyRecord> Map(
        IEnumerable<Dictionary<string, string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var results = new List<UnifiedAssemblyRecord>();

        foreach (var row in rows)
        {
            if (row is null)
                throw new InvalidDataException("Synthetic CSV contains a null row.");

            results.Add(MapSingleRecord(row));
        }

        return results;
    }

    private static UnifiedAssemblyRecord MapSingleRecord(
        Dictionary<string, string> row)
    {
        string simulationId = Required(row, "simulation_id");
        string family = Required(row, "assembly_family_group");
        string targetProvenance = Required(row, "target_provenance");
        string carbonProvenance = Required(row, "carbon_provenance");
        string costProvenance = Required(row, "cost_provenance");
        string compatibility = Required(row, "compatibility_status");

        bool synthetic = bool.Parse(Required(row, "synthetic_record"));

        if (!synthetic)
            throw new InvalidDataException(
                $"Scenario {simulationId} is not marked synthetic.");

        var warnings = new List<string>
        {
            "Synthetic scenario: not measured building performance.",
            "Carbon value is illustrative, not verified EPD data.",
            "Cost value is illustrative, not a market quotation.",
            "Compatibility has not been assessed.",
            "Do not combine with verified training labels."
        };

        var objectives = new AssemblyObjectiveRecord
        {
            AssemblyId = simulationId,
            AssemblyFamilyGroup = family,
            UValueWm2K = ParseDouble(row, "u_value_w_m2k"),
            EmbodiedCarbonA1A3KgCo2eM2 =
                ParseDouble(row, "embodied_carbon_a1_a3_kgco2e_m2"),
            MaterialCostEurM2 = ParseDouble(row, "material_cost_eur_m2"),
            TotalThicknessMm = ParseDouble(row, "total_thickness_mm"),
            TargetProvenance = targetProvenance,
            CarbonProvenance = carbonProvenance,
            CostProvenance = costProvenance,
            SyntheticRecord = true,
            CompatibilityStatus = compatibility
        };

        var provenance = new DataProvenance
        {
            DatasetPartition = DatasetPartition.SyntheticMultiObjectiveScenario,
            DatasetVersion = DatasetVersion,
            TargetProvenance = targetProvenance,
            CarbonProvenance = carbonProvenance,
            CostProvenance = costProvenance,
            VerificationStatus = "SYNTHETIC_NOT_VERIFIED",
            CompatibilityStatus = compatibility,
            SyntheticRecord = true,
            PreliminaryRecord = false,
            EvidenceIds = new List<string>(),
            Warnings = new List<string>(warnings)
        };

        return new UnifiedAssemblyRecord
        {
            AssemblyId = simulationId,
            AssemblyName = simulationId,
            AssemblyFamilyGroup = family,
            SubstrateProductId = Required(row, "substrate_id"),
            InsulationProductId = Required(row, "insulation_product_id"),
            InsulationVariantId = "",
            InteriorLiningProductId = Required(row, "lining_id"),
            Objectives = objectives,
            Provenance = provenance,
            ValidationWarnings = warnings
        };
    }

    private static string Required(
        Dictionary<string, string> row,
        string column)
    {
        if (!row.TryGetValue(column, out string? value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Required CSV value is missing: {column}");
        }

        return value.Trim();
    }

    private static double ParseDouble(
        Dictionary<string, string> row,
        string column)
    {
        string value = Required(row, column);

        if (!double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result) ||
            !double.IsFinite(result))
        {
            throw new InvalidDataException(
                $"Invalid numeric value in {column}: {value}");
        }

        return result;
    }
}
