using System;
using System.Collections.Generic;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective;

/// <summary>
/// Integrates mapped datasets into a common collection.
/// Applies structural validation and provenance eligibility policies.
/// Does not modify or recalculate source records.
/// </summary>
public sealed class UnifiedDatasetIntegrationService
{
    public UnifiedDatasetIntegrationReport Integrate(
        IEnumerable<UnifiedAssemblyRecord> records,
        DataUsePurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(records);

        var report = new UnifiedDatasetIntegrationReport();

        foreach (var record in records)
        {
            report.TotalRecordsReceived++;

            if (record is null)
            {
                Reject(
                    report,
                    "Null record received.");

                report.StructurallyInvalidRecords++;
                continue;
            }

            // -----------------------------------------
            // STEP 1 — Count dataset partition
            // -----------------------------------------

            if (record.Provenance is not null)
            {
                DatasetPartition partition =
                    record.Provenance.DatasetPartition;

                if (!report.PartitionCounts.ContainsKey(partition))
                    report.PartitionCounts[partition] = 0;

                report.PartitionCounts[partition]++;

                if (record.Provenance.SyntheticRecord)
                    report.SyntheticRecords++;
                else
                    report.NonSyntheticRecords++;

                if (record.Provenance.PreliminaryRecord)
                    report.PreliminaryRecords++;
            }

            // -----------------------------------------
            // STEP 2 — Structural validation
            // -----------------------------------------

            if (!record.IsStructurallyValid)
            {
                report.StructurallyInvalidRecords++;

                Reject(
                    report,
                    "Unified record is structurally incomplete.");

                continue;
            }

            report.StructurallyValidRecords++;

            // -----------------------------------------
            // STEP 3 — Provenance eligibility
            // -----------------------------------------

            bool eligible =
                DatasetProvenanceGuard.IsEligibleForPurpose(
                    record,
                    purpose.ToString(),
                    out string reason);

            if (!eligible)
            {
                Reject(report, reason);
                continue;
            }

            // -----------------------------------------
            // STEP 4 — Accept eligible record
            // -----------------------------------------

            report.IntegratedRecords.Add(record);
            report.EligibleRecords++;
        }

        // -----------------------------------------
        // STEP 5 — Final integration summary
        // -----------------------------------------

        report.RejectedRecords =
            report.TotalRecordsReceived - report.EligibleRecords;

        report.IsIntegrationSuccessful =
            report.StructurallyInvalidRecords == 0;

        if (report.EligibleRecords == 0)
        {
            report.Warnings.Add(
                "No records were eligible for the requested purpose.");
        }

        report.Warnings.Add(
            "Eligibility reflects declared provenance and policy rules; " +
            "it does not independently verify source documents.");

        report.Warnings.Add(
            "Different dataset partitions must retain their provenance " +
            "and must not be indiscriminately combined for model training.");

        return report;
    }

    private static void Reject(
        UnifiedDatasetIntegrationReport report,
        string reason)
    {
        if (!report.RejectionReasons.ContainsKey(reason))
            report.RejectionReasons[reason] = 0;

        report.RejectionReasons[reason]++;
    }
}