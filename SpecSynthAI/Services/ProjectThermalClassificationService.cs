using System;
using System.Collections.Generic;
using System.Linq;

using SpecSynthAI.Models;
using SpecSynthAI.Requirements;

using ThermalEngine.UI.Models;

namespace SpecSynthAI.Services
{
    public enum ThermalPerformanceStatus
    {
        Good,
        Moderate,
        Poor,
        NotAnalysed,
        Unsupported
    }

    public class WallClassificationResult
    {
        public string WallTypeName { get; set; } = "";

        public string WallFunction { get; set; } = "";

        public int InstanceCount { get; set; }

        public double? UValue { get; set; }

        public double? TargetUValue { get; set; }

        public double? ModerateLimit { get; set; }

        public ThermalPerformanceStatus Status { get; set; }

        public string Explanation { get; set; } = "";
    }

    public class ProjectThermalClassificationResult
    {
        public List<WallClassificationResult> Walls { get; set; } = new();

        // Each wall type contributes its actual placed instance count.

        public int GoodCount =>
            CountInstances(ThermalPerformanceStatus.Good);

        public int ModerateCount =>
            CountInstances(ThermalPerformanceStatus.Moderate);

        public int PoorCount =>
            CountInstances(ThermalPerformanceStatus.Poor);

        public int NotAnalysedCount =>
            CountInstances(ThermalPerformanceStatus.NotAnalysed);

        public int UnsupportedCount =>
            CountInstances(ThermalPerformanceStatus.Unsupported);

        public int CompliantCount => GoodCount;

        public int NeedsReviewCount =>
            ModerateCount +
            PoorCount +
            NotAnalysedCount +
            UnsupportedCount;

        public int TotalClassifiedInstances =>
            Walls.Sum(w => Math.Max(0, w.InstanceCount));

        private int CountInstances(
            ThermalPerformanceStatus status)
        {
            return Walls
                .Where(w => w.Status == status)
                .Sum(w => Math.Max(0, w.InstanceCount));
        }
    }

    public class ProjectThermalClassificationService
    {
        public ProjectThermalClassificationResult ClassifyProject(
            IEnumerable<ProjectWallRecord> wallRecords)
        {
            ArgumentNullException.ThrowIfNull(wallRecords);

            // =====================================
            // LOAD PROJECT REQUIREMENTS
            // =====================================

            ProjectRequirement externalRequirement =
                ProjectRequirementsStore.LoadExternalWallUValue();

            ProjectRequirement internalRequirement =
                ProjectRequirementsStore.LoadInternalWallUValue();

            ProjectRequirement toleranceRequirement =
                ProjectRequirementsStore.LoadModerateTolerance();

            double externalTarget =
                externalRequirement.MaximumAllowedValue;

            double internalTarget =
                internalRequirement.MaximumAllowedValue;

            double moderateTolerance =
                toleranceRequirement.MaximumAllowedValue;

            if (!IsValidRequirement(externalTarget) ||
                !IsValidRequirement(internalTarget) ||
                !IsValidTolerance(moderateTolerance))
            {
                throw new InvalidOperationException(
                    "Project thermal requirements contain invalid values.");
            }

            // =====================================
            // CLASSIFY WALL TYPES
            // =====================================

            var result = new ProjectThermalClassificationResult();

            foreach (ProjectWallRecord wall in wallRecords)
            {
                WallClassificationResult classification =
                    ClassifyWall(
                        wall,
                        externalTarget,
                        internalTarget,
                        moderateTolerance);

                result.Walls.Add(classification);

                // Update the existing model for WPF binding.

                wall.ThermalClassificationStatus =
                    GetDisplayStatus(classification.Status);

                wall.ThermalTargetUValue =
                    classification.TargetUValue;

                wall.ThermalClassificationExplanation =
                    classification.Explanation;
            }

            return result;
        }

        // =====================================
        // CLASSIFY INDIVIDUAL WALL TYPE
        // =====================================

        private static WallClassificationResult ClassifyWall(
            ProjectWallRecord wall,
            double externalTarget,
            double internalTarget,
            double moderateTolerance)
        {
            var classification = new WallClassificationResult
            {
                WallTypeName = wall.WallTypeName,
                WallFunction = wall.WallFunction,
                InstanceCount = Math.Max(0, wall.InstanceCount),
                UValue = wall.ThermalPerformance?.UValue
            };

            string function =
                wall.WallFunction?.Trim() ?? "";

            double target;

            // =====================================
            // DETERMINE WALL FUNCTION
            // =====================================

            if (function.Equals(
                "Exterior",
                StringComparison.OrdinalIgnoreCase))
            {
                target = externalTarget;
            }
            else if (function.Equals(
                "Interior",
                StringComparison.OrdinalIgnoreCase))
            {
                target = internalTarget;
            }
            else
            {
                classification.Status =
                    ThermalPerformanceStatus.Unsupported;

                classification.Explanation =
                    $"Wall function '{function}' is not currently supported.";

                return classification;
            }

            classification.TargetUValue = target;

            classification.ModerateLimit =
                target * (1.0 + moderateTolerance / 100.0);

            // =====================================
            // CHECK CALCULATION VALIDITY
            // =====================================

            if (wall.ThermalPerformance?.IsValid != true ||
                !wall.ThermalPerformance.UValue.HasValue)
            {
                classification.Status =
                    ThermalPerformanceStatus.NotAnalysed;

                classification.Explanation =
                    "A valid thermal U-value is unavailable.";

                return classification;
            }

            double uValue =
                wall.ThermalPerformance.UValue.Value;

            if (double.IsNaN(uValue) ||
                double.IsInfinity(uValue) ||
                uValue <= 0)
            {
                classification.Status =
                    ThermalPerformanceStatus.NotAnalysed;

                classification.Explanation =
                    "The calculated U-value is invalid.";

                return classification;
            }

            classification.UValue = uValue;

            // =====================================
            // APPLY PERFORMANCE THRESHOLDS
            // =====================================

            if (uValue <= target)
            {
                classification.Status =
                    ThermalPerformanceStatus.Good;

                classification.Explanation =
                    $"U-value meets the project target of {target:F3} W/(m²·K).";
            }
            else if (uValue <= classification.ModerateLimit.Value)
            {
                classification.Status =
                    ThermalPerformanceStatus.Moderate;

                classification.Explanation =
                    $"U-value exceeds the target but remains within the {moderateTolerance:F1}% tolerance.";
            }
            else
            {
                classification.Status =
                    ThermalPerformanceStatus.Poor;

                classification.Explanation =
                    $"U-value exceeds the permitted project tolerance. Target: {target:F3} W/(m²·K).";
            }

            return classification;
        }

        // =====================================
        // DISPLAY STATUS
        // =====================================

        private static string GetDisplayStatus(
            ThermalPerformanceStatus status)
        {
            return status switch
            {
                ThermalPerformanceStatus.Good =>
                    "GOOD",

                ThermalPerformanceStatus.Moderate =>
                    "MODERATE",

                ThermalPerformanceStatus.Poor =>
                    "POOR",

                ThermalPerformanceStatus.NotAnalysed =>
                    "NOT ANALYSED",

                ThermalPerformanceStatus.Unsupported =>
                    "UNSUPPORTED",

                _ => "NOT ANALYSED"
            };
        }

        // =====================================
        // VALIDATION
        // =====================================

        private static bool IsValidRequirement(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value > 0;
        }

        private static bool IsValidTolerance(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value >= 0;
        }
    }
}