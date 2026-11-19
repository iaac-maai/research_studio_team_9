using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration
{
    /// <summary>
    /// Read-only comparison of two saved optimisation runs.
    /// Reports selected objectives, Pareto-set changes and metric values
    /// for alternatives shared by both runs.
    /// </summary>
    public sealed class OptimisationRunComparisonService
    {
        public OptimisationRunComparison Compare(
            OptimisationResult runA,
            OptimisationResult runB)
        {
            ArgumentNullException.ThrowIfNull(runA);
            ArgumentNullException.ThrowIfNull(runB);

            if (runA.Solutions == null || runB.Solutions == null)
                throw new ArgumentException(
                    "Both runs must contain solution collections.");

            var comparison = new OptimisationRunComparison
            {
                RunARequestId = runA.RequestId,
                RunBRequestId = runB.RequestId,

                RunACandidateCount = runA.Summary.CandidateCount,
                RunBCandidateCount = runB.Summary.CandidateCount,

                RunAParetoCount = runA.Solutions.Count,
                RunBParetoCount = runB.Solutions.Count,

                RunAOutOfDomainCount =
                    runA.Solutions.Count(x => x.ModelOutOfDomain),

                RunBOutOfDomainCount =
                    runB.Solutions.Count(x => x.ModelOutOfDomain),

                RunASelectedObjectives = ReadSelectedObjectives(runA),
                RunBSelectedObjectives = ReadSelectedObjectives(runB)
            };

            // These minima are independent. They may belong to different
            // Pareto solutions and must not be presented as one combined design.
            comparison.RunABestUValue = GetMinimum(
                runA.Solutions.Select(x => (double?)x.UValueWm2K));
            comparison.RunBBestUValue = GetMinimum(
                runB.Solutions.Select(x => (double?)x.UValueWm2K));

            comparison.RunABestCarbon = GetMinimum(
                runA.Solutions.Select(x => x.CarbonKgCo2eM2));
            comparison.RunBBestCarbon = GetMinimum(
                runB.Solutions.Select(x => x.CarbonKgCo2eM2));

            comparison.RunABestCost = GetMinimum(
                runA.Solutions.Select(x => x.CostEurM2));
            comparison.RunBBestCost = GetMinimum(
                runB.Solutions.Select(x => x.CostEurM2));

            var alternativesA = GetAlternativeMap(runA);
            var alternativesB = GetAlternativeMap(runB);

            comparison.CommonAlternatives = alternativesA.Keys
                .Intersect(alternativesB.Keys, StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            comparison.NewAlternatives = alternativesB.Keys
                .Except(alternativesA.Keys, StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            comparison.RemovedAlternatives = alternativesA.Keys
                .Except(alternativesB.Keys, StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            foreach (string key in comparison.CommonAlternatives)
            {
                OptimisationSolution a = alternativesA[key];
                OptimisationSolution b = alternativesB[key];

                comparison.CommonAlternativeDetails.Add(
                    new OptimisationAlternativeComparison
                    {
                        Key = key,
                        MaterialId = a.MaterialId,
                        MaterialLabel = string.IsNullOrWhiteSpace(a.MaterialLabel)
                            ? b.MaterialLabel
                            : a.MaterialLabel,
                        ThicknessMm = a.ThicknessMm,

                        RunAUValue = a.UValueWm2K,
                        RunBUValue = b.UValueWm2K,
                        RunACarbon = a.CarbonKgCo2eM2,
                        RunBCarbon = b.CarbonKgCo2eM2,
                        RunACost = a.CostEurM2,
                        RunBCost = b.CostEurM2,

                        RunAOutOfDomain = a.ModelOutOfDomain,
                        RunBOutOfDomain = b.ModelOutOfDomain
                    });
            }

            return comparison;
        }

        private static List<string> ReadSelectedObjectives(
            OptimisationResult result)
        {
            var report = result.Provenance.Stage7_2Report;

            if (report.ValueKind != JsonValueKind.Object ||
                !report.TryGetProperty("selected_objectives", out var objectives) ||
                objectives.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return objectives.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static double? GetMinimum(IEnumerable<double?> values)
        {
            var validValues = values
                .Where(x => x.HasValue &&
                            double.IsFinite(x.Value) &&
                            x.Value >= 0)
                .Select(x => x!.Value)
                .ToList();

            return validValues.Count == 0 ? null : validValues.Min();
        }

        private static Dictionary<string, OptimisationSolution> GetAlternativeMap(
            OptimisationResult result)
        {
            // If duplicate material/thickness rows exist, retain the first.
            return result.Solutions
                .Where(x => !string.IsNullOrWhiteSpace(x.MaterialId))
                .GroupBy(
                    x => $"{x.MaterialId.Trim().ToUpperInvariant()}|{x.ThicknessMm:F2}",
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);
        }
    }

    public sealed class OptimisationRunComparison
    {
        public string RunARequestId { get; set; } = string.Empty;
        public string RunBRequestId { get; set; } = string.Empty;

        public int RunACandidateCount { get; set; }
        public int RunBCandidateCount { get; set; }

        public int RunAParetoCount { get; set; }
        public int RunBParetoCount { get; set; }

        public int RunAOutOfDomainCount { get; set; }
        public int RunBOutOfDomainCount { get; set; }

        public List<string> RunASelectedObjectives { get; set; } = new();
        public List<string> RunBSelectedObjectives { get; set; } = new();

        public double? RunABestUValue { get; set; }
        public double? RunBBestUValue { get; set; }

        public double? RunABestCarbon { get; set; }
        public double? RunBBestCarbon { get; set; }

        public double? RunABestCost { get; set; }
        public double? RunBBestCost { get; set; }

        public List<string> CommonAlternatives { get; set; } = new();
        public List<string> NewAlternatives { get; set; } = new();
        public List<string> RemovedAlternatives { get; set; } = new();

        public List<OptimisationAlternativeComparison>
            CommonAlternativeDetails
        { get; set; } = new();
    }

    public sealed class OptimisationAlternativeComparison
    {
        public string Key { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialLabel { get; set; } = string.Empty;
        public double ThicknessMm { get; set; }

        public double RunAUValue { get; set; }
        public double RunBUValue { get; set; }

        public double? RunACarbon { get; set; }
        public double? RunBCarbon { get; set; }

        public double? RunACost { get; set; }
        public double? RunBCost { get; set; }

        public bool RunAOutOfDomain { get; set; }
        public bool RunBOutOfDomain { get; set; }
    }
}
