using System;

namespace SpecSynthAI.Models
{
    /// <summary>
    /// Visual status used by the SpecSynth spatial compliance heatmap.
    /// This class classifies existing inspection outputs; it does not
    /// perform or replace compliance calculations.
    /// </summary>
    public enum ComplianceVisualStatus
    {
        Pass,
        Fail,
        Warning,
        Information
    }

    public class ComplianceVisualResult
    {
        public ComplianceVisualStatus Status { get; set; }

        public string Label { get; set; } = "";

        public string Explanation { get; set; } = "";
    }

    public static class ComplianceVisualStatusClassifier
    {
        /// <summary>
        /// Converts existing product verification, thickness and thermal
        /// check results into one conservative visual status.
        ///
        /// Priority:
        /// 1. No mapped product -> Information (blue)
        /// 2. Product not verified -> Warning (amber)
        /// 3. Confirmed failure in a verified product -> Fail (red)
        /// 4. All implemented checks pass -> Pass (green)
        /// 5. Incomplete evidence -> Information (blue)
        /// </summary>
        public static ComplianceVisualResult Classify(
            Product? product,
            string thicknessMatch,
            string thermalCheck)
        {
            if (product == null)
            {
                return Create(
                    ComplianceVisualStatus.Information,
                    "CANNOT VERIFY",
                    "No catalogue product is mapped to this Revit material.");
            }

            bool isVerified = string.Equals(
                product.VerificationStatus?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase);

            if (!isVerified)
            {
                return Create(
                    ComplianceVisualStatus.Warning,
                    "WARNING",
                    "The mapped product is not fully verified. Compliance cannot be confirmed.");
            }

            bool thicknessFailed = string.Equals(
                thicknessMatch?.Trim(),
                "NO EXACT MATCH",
                StringComparison.OrdinalIgnoreCase);

            bool thermalFailed = string.Equals(
                thermalCheck?.Trim(),
                "FAIL",
                StringComparison.OrdinalIgnoreCase);

            if (thicknessFailed || thermalFailed)
            {
                string reason = "";

                if (thicknessFailed && thermalFailed)
                {
                    reason = "Installed thickness does not match the documented options, and thermal conductivity exceeds the project limit.";
                }
                else if (thicknessFailed)
                {
                    reason = "Installed thickness does not match the documented product thickness options.";
                }
                else
                {
                    reason = "Thermal conductivity exceeds the configured project limit.";
                }

                return Create(
                    ComplianceVisualStatus.Fail,
                    "FAIL",
                    reason);
            }

            bool thicknessPassed = string.Equals(
                thicknessMatch?.Trim(),
                "EXACT MATCH",
                StringComparison.OrdinalIgnoreCase);

            bool thermalPassed = string.Equals(
                thermalCheck?.Trim(),
                "PASS",
                StringComparison.OrdinalIgnoreCase);

            if (thicknessPassed && thermalPassed)
            {
                return Create(
                    ComplianceVisualStatus.Pass,
                    "PASS",
                    "All currently implemented checks passed.");
            }

            return Create(
                ComplianceVisualStatus.Information,
                "CANNOT VERIFY",
                "The product is verified, but one or more required checks do not have enough data for a conclusive result.");
        }

        private static ComplianceVisualResult Create(
            ComplianceVisualStatus status,
            string label,
            string explanation)
        {
            return new ComplianceVisualResult
            {
                Status = status,
                Label = label,
                Explanation = explanation
            };
        }
    }
}
