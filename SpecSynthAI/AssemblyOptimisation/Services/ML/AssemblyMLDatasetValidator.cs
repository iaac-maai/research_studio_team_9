using System;
using System.Collections.Generic;
using System.Linq;
using SpecSynthAI.AssemblyOptimisation.Models.ML;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

public sealed class AssemblyMLDatasetValidationReport
{
    public int RecordsReceived { get; set; }
    public int RecordsWithValidTarget { get; set; }
    public int RecordsWithCompleteConductivityFeatures { get; set; }
    public int UniqueAssemblyFamilies { get; set; }
    public int DuplicateRecordIds { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class AssemblyMLDatasetValidator
{
    public AssemblyMLDatasetValidationReport Validate(
        IReadOnlyCollection<AssemblyMLRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var report = new AssemblyMLDatasetValidationReport
        {
            RecordsReceived = records.Count,
            RecordsWithValidTarget = records.Count(r =>
                r.IsThermalCalculationValid &&
                r.UValue_Wm2K.HasValue &&
                double.IsFinite(r.UValue_Wm2K.Value) &&
                r.UValue_Wm2K.Value > 0),
            RecordsWithCompleteConductivityFeatures =
                records.Count(r => r.HasCompleteConductivityFeatures),
            UniqueAssemblyFamilies = records
                .Select(r => r.AssemblyFamilyGroup)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            DuplicateRecordIds = records
                .GroupBy(r => r.RecordId, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Count() > 1)
        };

        if (report.RecordsReceived == 0)
            report.Warnings.Add("No ML records were generated.");

        if (report.DuplicateRecordIds > 0)
            report.Warnings.Add("Duplicate RecordId values detected.");

        if (report.UniqueAssemblyFamilies < 2)
            report.Warnings.Add(
                "Fewer than two independent assembly families: a reliable grouped train/test split is not possible.");

        if (report.RecordsWithCompleteConductivityFeatures < report.RecordsReceived)
            report.Warnings.Add(
                "Some rows have incomplete or non-homogeneous conductivity features. Preserve nulls; do not impute without a documented strategy.");

        report.Warnings.Add(
            "Target U-values are deterministic ThermalEngine outputs, not measured building outcomes.");

        report.Warnings.Add(
            "Do not randomly split rows from the same AssemblyFamilyGroup across train and test.");

        return report;
    }
}
