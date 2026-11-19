using SpecSynthAI.AssemblyOptimisation.Models.ML;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

/// <summary>
/// Maps ThermalEngine baseline ML records into the common unified contract.
/// Does not recalculate or modify source values.
/// </summary>
public sealed class AssemblyMLRecordMapper
{
    private const string DatasetVersion = "thermal_surrogate_baseline_v2";

    public List<UnifiedAssemblyRecord> Map(
        IEnumerable<AssemblyMLRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var unifiedRecords = new List<UnifiedAssemblyRecord>();

        foreach (var record in records)
        {
            if (record is null)
                continue;

            unifiedRecords.Add(MapSingleRecord(record));
        }

        return unifiedRecords;
    }

    private static UnifiedAssemblyRecord MapSingleRecord(
        AssemblyMLRecord record)
    {
        string assemblyId = !string.IsNullOrWhiteSpace(record.CandidateId)
            ? record.CandidateId
            : record.RecordId;

        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(record.CandidateId))
            warnings.Add("CandidateId is unavailable; RecordId used as AssemblyId.");

        if (!record.IsThermalCalculationValid)
            warnings.Add("Thermal calculation is marked invalid.");

        if (!record.HasCompleteConductivityFeatures)
            warnings.Add("Conductivity features are incomplete.");

        if (!string.IsNullOrWhiteSpace(record.TrainingUse))
            warnings.Add($"Training restriction: {record.TrainingUse}");

        warnings.Add("Carbon objective is unavailable in the thermal baseline dataset.");
        warnings.Add("Cost objective is unavailable in the thermal baseline dataset.");

        var objectives = new AssemblyObjectiveRecord
        {
            AssemblyId = assemblyId,
            AssemblyFamilyGroup = record.AssemblyFamilyGroup,
            UValueWm2K = record.UValue_Wm2K,
            TotalThicknessMm = record.TotalThicknessMm,
            EmbodiedCarbonA1A3KgCo2eM2 = null,
            MaterialCostEurM2 = null,
            TargetProvenance = record.TargetProvenance,
            CarbonProvenance = "NOT_AVAILABLE",
            CostProvenance = "NOT_AVAILABLE",
            SyntheticRecord = false,
            CompatibilityStatus = "NOT_ASSESSED"
        };

        var provenance = new DataProvenance
        {
            DatasetPartition = DatasetPartition.ThermalEngineBaseline,
            DatasetVersion = DatasetVersion,
            TargetProvenance = record.TargetProvenance,
            CarbonProvenance = "NOT_AVAILABLE",
            CostProvenance = "NOT_AVAILABLE",
            VerificationStatus = "THERMALENGINE_CALCULATED_NOT_MEASURED",
            CompatibilityStatus = "NOT_ASSESSED",
            SyntheticRecord = false,
            PreliminaryRecord = false,
            EvidenceIds = record.SourceIds?.ToList() ?? new List<string>(),
            Warnings = new List<string>(warnings)
        };

        return new UnifiedAssemblyRecord
        {
            AssemblyId = assemblyId,
            AssemblyName = record.AssemblyName,
            AssemblyFamilyGroup = record.AssemblyFamilyGroup,
            SubstrateProductId = record.SubstrateProductId,
            InsulationProductId = record.InsulationProductId,
            InsulationVariantId = record.InsulationVariantId,
            InteriorLiningProductId = record.InteriorLiningProductId,
            Objectives = objectives,
            Provenance = provenance,
            ValidationWarnings = warnings
        };
    }
}
