using System;
using SpecSynthAI.Models;

namespace SpecSynthAI.Requirements
{
    public class ComplianceChecker
    {
        public static string CheckThermalConductivity(
        Product product,
        ProjectRequirement requirement)
        {
            // 1. Check whether the required objects exist.
            if (product == null || requirement == null)
            {
                return "CANNOT VERIFY";
            }

        // 2. Product data must have a verified status.
        string status = product.VerificationStatus?.Trim() ?? "";

            bool isVerified = string.Equals(
                status,
                "verified",
                StringComparison.OrdinalIgnoreCase
            );

            if (!isVerified)
            {
                return "CANNOT VERIFY";
            }

            // 3. Check whether thermal conductivity is available.
            double? conductivityValue = product.ThermalConductivityWmK;

            if (!conductivityValue.HasValue)
            {
                return "CANNOT VERIFY";
            }

            double conductivity = conductivityValue.Value;

            if (double.IsNaN(conductivity) ||
                double.IsInfinity(conductivity) ||
                conductivity <= 0)
            {
                return "CANNOT VERIFY";
            }

            // 4. Check whether the requirement is valid.
            double maximumAllowed = requirement.MaximumAllowedValue;

            if (double.IsNaN(maximumAllowed) ||
                double.IsInfinity(maximumAllowed) ||
                maximumAllowed <= 0)
            {
                return "CANNOT VERIFY";
            }

            // 5. Compare the verified product value with the requirement.
            if (conductivity <= maximumAllowed)
            {
                return "PASS";
            }

            return "FAIL";
        }
    }

}
