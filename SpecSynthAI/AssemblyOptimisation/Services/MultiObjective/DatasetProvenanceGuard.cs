using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective;

/// <summary>
/// Central policy gate for dataset use based on declared provenance.
/// It does not verify source documents, product identity, EPD pages,
/// or regulatory compliance.
/// </summary>
public static class DatasetProvenanceGuard
{
    private const string EmpiricalTarget =
        "MEASURED_OR_INDEPENDENTLY_VALIDATED_SIMULATION";

    /// <summary>
    /// Retains the existing API used by earlier product-release code.
    /// </summary>
    public static void EnsureEligibleForVerifiedRelease(
        string dataClass,
        bool syntheticRecord)
    {
        if (syntheticRecord ||
            string.Equals(dataClass, "SYNTHETIC_SCENARIO_ONLY",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataClass, "SYNTHETIC_SCENARIO_SIMULATION",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Synthetic scenario data cannot enter the verified product release.");
        }
    }

    /// <summary>
    /// Empirical training requires a measured or independently validated target.
    /// A deterministic ThermalEngine surrogate does not pass this gate.
    /// </summary>
    public static bool IsEligibleForEmpiricalModelTraining(
        string targetProvenance,
        bool syntheticRecord)
    {
        return !syntheticRecord &&
               string.Equals(
                   targetProvenance,
                   EmpiricalTarget,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsEligibleForPurpose(
        UnifiedAssemblyRecord record,
        string purpose,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!record.IsStructurallyValid)
        {
            reason = "Unified record is structurally incomplete.";
            return false;
        }

        return IsEligibleForPartition(
            record.Provenance.DatasetPartition,
            record.Provenance.SyntheticRecord,
            record.Provenance.PreliminaryRecord,
            record.Provenance.VerificationStatus,
            purpose,
            out reason,
            record.Provenance.TargetProvenance);
    }

    public static bool IsEligibleForPartition(
        DatasetPartition partition,
        bool syntheticRecord,
        bool preliminaryRecord,
        string? verificationStatus,
        string purpose,
        out string reason,
        string? targetProvenance = null)
    {
        string requestedPurpose = purpose?.Trim() ?? "";

        if (partition == DatasetPartition.Unknown)
        {
            reason = "Unknown dataset partition is blocked.";
            return false;
        }

        if (EqualsPurpose(requestedPurpose, "EmpiricalModelTraining"))
        {
            bool eligible =
                !syntheticRecord &&
                !preliminaryRecord &&
                IsEligibleForEmpiricalModelTraining(
                    targetProvenance ?? "", syntheticRecord);

            reason = eligible
                ? "Measured or independently validated target accepted."
                : "Empirical training requires non-synthetic, non-preliminary records with measured or independently validated targets.";
            return eligible;
        }

        switch (partition)
        {
            case DatasetPartition.VerifiedProductCatalogue:
            {
                bool verified =
                    EqualsStatus(verificationStatus, "VERIFIED") ||
                    EqualsStatus(verificationStatus, "MANUFACTURER_VERIFIED");

                bool allowed =
                    !syntheticRecord &&
                    !preliminaryRecord &&
                    verified &&
                    (EqualsPurpose(requestedPurpose, "ProductPropertyAnalysis") ||
                     EqualsPurpose(requestedPurpose, "VerifiedProductRelease"));

                reason = allowed
                    ? "Verified product-property use accepted."
                    : "Verified catalogue requires verified evidence and a permitted product purpose.";
                return allowed;
            }

            case DatasetPartition.ThermalEngineBaseline:
            {
                bool allowed =
                    !syntheticRecord &&
                    !preliminaryRecord &&
                    (EqualsPurpose(requestedPurpose, "BaselineExperiment") ||
                     EqualsPurpose(requestedPurpose, "PipelineValidation"));

                reason = allowed
                    ? "ThermalEngine baseline use accepted; grouped splitting is required for experiments."
                    : "ThermalEngine baseline is restricted to baseline experiments and pipeline validation.";
                return allowed;
            }

            case DatasetPartition.SyntheticMultiObjectiveScenario:
            case DatasetPartition.SyntheticProductCatalogue:
            {
                bool allowed =
                    syntheticRecord &&
                    !preliminaryRecord &&
                    (EqualsPurpose(requestedPurpose, "PipelineValidation") ||
                     EqualsPurpose(requestedPurpose, "SensitivityAnalysis") ||
                     EqualsPurpose(requestedPurpose, "OptimisationDemonstration"));

                reason = allowed
                    ? "Synthetic demonstration use accepted; results remain illustrative."
                    : "Synthetic data is blocked from verified release and empirical training.";
                return allowed;
            }

            case DatasetPartition.PreliminaryEPD:
            {
                bool allowed =
                    !syntheticRecord &&
                    preliminaryRecord &&
                    EqualsPurpose(requestedPurpose, "PreliminaryEvidenceReview");

                reason = allowed
                    ? "Preliminary EPD manual review accepted; this is not release or training approval."
                    : "Preliminary EPD is restricted to manual evidence review.";
                return allowed;
            }

            default:
                reason = "No policy exists for this dataset partition.";
                return false;
        }
    }

    public static void EnsureEligibleForVerifiedRelease(
        UnifiedAssemblyRecord record)
    {
        if (!IsEligibleForPurpose(
                record, "VerifiedProductRelease", out string reason))
        {
            throw new InvalidOperationException(reason);
        }
    }

    private static bool EqualsPurpose(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool EqualsStatus(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
}
