using System;
using System.Collections.Generic;
using System.Linq;
using SpecSynthAI.AssemblyOptimisation.Models;
using SpecSynthAI.AssemblyOptimisation.Models.ML;
using ThermalEngine.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

/// <summary>
/// Converts already-generated candidates and their already-calculated
/// ThermalEngine results into ML records. Does not rerun ThermalEngine.
/// </summary>
public sealed class AssemblyMLDatasetGenerator
{
    public List<AssemblyMLRecord> Generate(
        AssemblyGenerationReport generationReport,
        AssemblyEvaluationReport evaluationReport)
    {
        ArgumentNullException.ThrowIfNull(generationReport);
        ArgumentNullException.ThrowIfNull(evaluationReport);

        var evaluatedById = evaluationReport.RankedCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x.CandidateId))
            .GroupBy(x => x.CandidateId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(),
                StringComparer.OrdinalIgnoreCase);

        var records = new List<AssemblyMLRecord>();

        foreach (AssemblyCandidate candidate in generationReport.Candidates)
        {
            if (!evaluatedById.TryGetValue(candidate.CandidateId, out AssemblyEvaluationItem? item))
                continue;

            WallComparisonResult thermal = item.ThermalResult;
            bool validTarget =
                thermal.IsValid &&
                thermal.UValue.HasValue &&
                double.IsFinite(thermal.UValue.Value) &&
                thermal.UValue.Value > 0;

            if (!validTarget)
                continue;

            List<ThermalLayer> layers = candidate.Assembly.Layers;
            var warnings = new List<string>();

            if (layers.Count != 3)
                warnings.Add($"Expected 3 layers from current generator; found {layers.Count}.");

            var validConductivities = layers
                .Where(l => IsPositiveFinite(l.ThermalConductivityWmK))
                // Equivalent-wall conductivity must not be interpreted as
                // homogeneous material conductivity.
                .Where(l => l.ResistanceScope != ResistanceValueScope.EquivalentWallOrAssembly)
                .Select(l => l.ThermalConductivityWmK!.Value)
                .ToList();

            var insulationLayer = layers.FirstOrDefault(l =>
                !string.IsNullOrWhiteSpace(l.ProductId) &&
                string.Equals(l.ProductId, candidate.InsulationProductId,
                    StringComparison.OrdinalIgnoreCase));

            string familyGroup = string.Join("|",
                candidate.SubstrateProductId,
                candidate.InsulationProductId,
                candidate.InteriorLiningProductId);

            records.Add(new AssemblyMLRecord
            {
                RecordId = candidate.CandidateId,
                CandidateId = candidate.CandidateId,
                AssemblyName = candidate.AssemblyName,
                CandidateStatus = candidate.CandidateStatus,
                AssemblyFamilyGroup = familyGroup,

                SubstrateProductId = candidate.SubstrateProductId,
                InsulationProductId = candidate.InsulationProductId,
                InsulationVariantId = candidate.InsulationVariantId,
                InteriorLiningProductId = candidate.InteriorLiningProductId,

                NumberOfLayers = layers.Count,
                TotalThicknessMm = layers.Sum(l => Math.Max(0, l.ThicknessMm)),
                MinConductivityWmK = validConductivities.Count > 0 ? validConductivities.Min() : null,
                MaxConductivityWmK = validConductivities.Count > 0 ? validConductivities.Max() : null,
                MeanConductivityWmK = validConductivities.Count > 0 ? validConductivities.Average() : null,
                InsulationThicknessMm = insulationLayer?.ThicknessMm,

                Layer1ThicknessMm = GetThickness(layers, 0),
                Layer1ConductivityWmK = GetModelConductivity(layers, 0),
                Layer2ThicknessMm = GetThickness(layers, 1),
                Layer2ConductivityWmK = GetModelConductivity(layers, 1),
                Layer3ThicknessMm = GetThickness(layers, 2),
                Layer3ConductivityWmK = GetModelConductivity(layers, 2),

                TotalResistanceM2K_W = thermal.TotalResistance,
                UValue_Wm2K = thermal.UValue,
                IsThermalCalculationValid = validTarget,
                HasCompleteConductivityFeatures =
                    layers.Count == 3 && layers.All(l => IsPositiveFinite(l.ThermalConductivityWmK)
                        && l.ResistanceScope != ResistanceValueScope.EquivalentWallOrAssembly),
                ConductivityProvenanceSummary = string.Join("|", layers.Select(l => l.ConductivitySource)),
                ResistanceScopeSummary = string.Join("|", layers.Select(l => l.ResistanceScope)),
                SourceIds = candidate.SourceIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                QualityWarnings = warnings
            });
        }

        return records;
    }

    private static bool IsPositiveFinite(double? value) =>
        value.HasValue && double.IsFinite(value.Value) && value.Value > 0;

    private static double? GetThickness(IReadOnlyList<ThermalLayer> layers, int index) =>
        layers.Count > index ? layers[index].ThicknessMm : null;

    private static double? GetModelConductivity(IReadOnlyList<ThermalLayer> layers, int index)
    {
        if (layers.Count <= index) return null;
        ThermalLayer layer = layers[index];
        return layer.ResistanceScope == ResistanceValueScope.EquivalentWallOrAssembly
            ? null
            : IsPositiveFinite(layer.ThermalConductivityWmK)
                ? layer.ThermalConductivityWmK
                : null;
    }
}
