using System;
using System.Collections.Generic;
using System.Linq;

using SpecSynthAI.AssemblyOptimisation.Models;

using ThermalEngine.Models;
using ThermalEngine.Services;

namespace SpecSynthAI.AssemblyOptimisation.Services;

/// <summary>
/// Evaluates wall assemblies using the existing ThermalEngine.
/// Supports generated candidates and imported assembly datasets.
/// </summary>
public sealed class AssemblyEvaluationService
{
    private readonly WallComparisonService _comparisonService;

    public AssemblyEvaluationService()
    {
        _comparisonService = new WallComparisonService();
    }


    // =========================================================
    // PIPELINE 1 — EXISTING GENERATED CANDIDATES
    // =========================================================

    public AssemblyEvaluationReport Evaluate(
        AssemblyGenerationReport generationReport)
    {
        ArgumentNullException.ThrowIfNull(generationReport);

        var report = new AssemblyEvaluationReport
        {
            DatasetVersion = generationReport.DatasetVersion,
            CandidatesReceived = generationReport.Candidates.Count,
            Warnings = new List<string>(generationReport.Warnings)
        };

        if (generationReport.Candidates.Count == 0)
        {
            report.Warnings.Add(
                "No candidates were supplied for thermal evaluation.");

            return report;
        }

        // Convert generated candidates into WallAssembly objects.
        WallAssembly[] assemblies = generationReport.Candidates
            .Select(candidate => candidate.Assembly)
            .ToArray();

        // Existing ThermalEngine calculation service.
        WallComparisonReport comparison =
            _comparisonService.CompareAssemblies(assemblies);

        report.ComparisonReport = comparison;

        // Match thermal results to candidate names.
        var resultLookup = comparison.Results
            .GroupBy(
                result => result.AssemblyName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new Queue<WallComparisonResult>(group),
                StringComparer.OrdinalIgnoreCase);

        foreach (AssemblyCandidate candidate in generationReport.Candidates)
        {
            WallComparisonResult result;

            if (resultLookup.TryGetValue(
                    candidate.AssemblyName,
                    out Queue<WallComparisonResult>? queue)
                && queue.Count > 0)
            {
                result = queue.Dequeue();
            }
            else
            {
                result = new WallComparisonResult
                {
                    AssemblyName = candidate.AssemblyName,
                    IsValid = false,
                    Errors = new List<string>
                    {
                        "No matching result was returned by WallComparisonService."
                    }
                };
            }

            report.RankedCandidates.Add(new AssemblyEvaluationItem
            {
                CandidateId = candidate.CandidateId,
                AssemblyName = candidate.AssemblyName,
                CandidateStatus = candidate.CandidateStatus,
                CompatibilityNote = candidate.CompatibilityNote,
                ThermalResult = result
            });
        }

        // Rank valid results first, then lowest U-value.
        report.RankedCandidates = report.RankedCandidates
            .OrderBy(item => item.ThermalResult.IsValid ? 0 : 1)
            .ThenBy(item =>
                item.ThermalResult.UValue ?? double.MaxValue)
            .ToList();

        report.ValidCalculations = report.RankedCandidates.Count(
            item =>
                item.ThermalResult.IsValid &&
                item.ThermalResult.UValue.HasValue);

        report.InvalidCalculations =
            report.CandidatesReceived - report.ValidCalculations;

        AssemblyEvaluationItem? best = report.RankedCandidates
            .FirstOrDefault(item =>
                item.ThermalResult.IsValid &&
                item.ThermalResult.UValue.HasValue);

        if (best is not null)
        {
            report.BestPerformingCandidateId = best.CandidateId;
            report.BestPerformingAssembly = best.AssemblyName;
            report.LowestUValue = best.ThermalResult.UValue;
        }

        report.Warnings.Add(
            "Ranking is based only on calculated U-value. It does not assess " +
            "system compatibility, moisture, thermal bridges, structural safety, " +
            "fire strategy, cost, embodied carbon or regulatory compliance.");

        return report;
    }


    // =========================================================
    // PIPELINE 2 — IMPORTED ASSEMBLY DATASET
    // =========================================================

    public AssemblyEvaluationReport Evaluate(
        AssemblyDatasetMappingReport mappingReport)
    {
        ArgumentNullException.ThrowIfNull(mappingReport);

        var report = new AssemblyEvaluationReport
        {
            DatasetId = mappingReport.DatasetId,
            SchemaVersion = mappingReport.SchemaVersion,

            // The mapper currently exposes schema_version,
            // not a separate dataset_version.
            DatasetVersion = mappingReport.SchemaVersion,

            AssembliesReceived = mappingReport.TotalAssemblies,

            AssembliesReadyForCalculation =
                mappingReport.CalculationReadyCount,

            AssembliesNotReadyForCalculation =
                mappingReport.NotReadyCount
        };


        // -----------------------------------------------------
        // STEP 1 — Preserve mapping results
        // -----------------------------------------------------

        foreach (AssemblyDatasetMappingItem mappingItem
                 in mappingReport.AssemblyResults)
        {
            var evaluationItem = new AssemblyDatasetEvaluationItem
            {
                AssemblyId = mappingItem.AssemblyId,

                AssemblyName = mappingItem.AssemblyName,

                IsMappingReady = mappingItem.IsCalculationReady,

                MappingErrors = new List<string>(
                    mappingItem.Errors),

                MappingWarnings = new List<string>(
                    mappingItem.Warnings),

                HasValidThermalResult = false,

                UValue = null
            };

            report.DatasetAssemblies.Add(evaluationItem);
        }


        // -----------------------------------------------------
        // STEP 2 — Stop if no assemblies are calculation-ready
        // -----------------------------------------------------

        if (mappingReport.CalculationReadyAssemblies.Count == 0)
        {
            report.Warnings.Add(
                "No calculation-ready assemblies were supplied. " +
                "Thermal evaluation was not performed.");

            return report;
        }


        // -----------------------------------------------------
        // STEP 3 — Run ThermalEngine
        // -----------------------------------------------------

        WallComparisonReport comparison =
            _comparisonService.CompareAssemblies(
                mappingReport.CalculationReadyAssemblies);

        report.ComparisonReport = comparison;


        // -----------------------------------------------------
        // STEP 4 — Match thermal results to assembly names
        // -----------------------------------------------------

        var resultLookup = comparison.Results
            .GroupBy(
                result => result.AssemblyName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new Queue<WallComparisonResult>(group),
                StringComparer.OrdinalIgnoreCase);


        foreach (AssemblyDatasetEvaluationItem item
                 in report.DatasetAssemblies)
        {
            // Do not calculate assemblies that failed mapping.
            if (!item.IsMappingReady)
                continue;

            if (resultLookup.TryGetValue(
                    item.AssemblyName,
                    out Queue<WallComparisonResult>? queue)
                && queue.Count > 0)
            {
                WallComparisonResult result = queue.Dequeue();

                item.ThermalResult = result;

                item.HasValidThermalResult =
                    result.IsValid &&
                    result.UValue.HasValue &&
                    double.IsFinite(result.UValue.Value) &&
                    result.UValue.Value > 0;

                item.UValue = item.HasValidThermalResult
                    ? result.UValue
                    : null;
            }
            else
            {
                item.HasValidThermalResult = false;

                item.UValue = null;

                item.MappingWarnings.Add(
                    "No matching thermal result was returned by WallComparisonService.");
            }
        }


        // -----------------------------------------------------
        // STEP 5 — Calculate evaluation counts
        // -----------------------------------------------------

        report.CandidatesReceived =
            mappingReport.CalculationReadyCount;

        report.ValidCalculations = report.DatasetAssemblies.Count(
            item => item.HasValidThermalResult);

        report.InvalidCalculations =
            report.CandidatesReceived - report.ValidCalculations;


        // -----------------------------------------------------
        // STEP 6 — Rank imported assemblies
        // -----------------------------------------------------

        report.RankedDatasetAssemblies = report.DatasetAssemblies
            .Where(item =>
                item.HasValidThermalResult &&
                item.UValue.HasValue)
            .OrderBy(item => item.UValue!.Value)
            .ToList();


        // -----------------------------------------------------
        // STEP 7 — Identify best-performing assembly
        // -----------------------------------------------------

        AssemblyDatasetEvaluationItem? best =
            report.RankedDatasetAssemblies.FirstOrDefault();

        if (best is not null)
        {
            report.BestPerformingAssemblyId = best.AssemblyId;

            report.BestPerformingAssembly = best.AssemblyName;

            report.BestPerformingCandidateId = null;

            report.LowestUValue = best.UValue;
        }


        // -----------------------------------------------------
        // STEP 8 — Add evaluation limitations
        // -----------------------------------------------------

        report.Warnings.Add(
            "Thermal ranking considers calculated U-value only. " +
            "It does not establish system compatibility, moisture safety, " +
            "thermal-bridge performance, structural suitability, fire compliance, " +
            "cost, embodied carbon or regulatory compliance.");

        return report;
    }
}