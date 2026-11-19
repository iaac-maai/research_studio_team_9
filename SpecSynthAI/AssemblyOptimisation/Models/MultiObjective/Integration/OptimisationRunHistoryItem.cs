using System;

namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration
{
    /// <summary>
    /// Represents one previously executed and saved optimisation run.
    /// </summary>
    public sealed class OptimisationRunHistoryItem
    {
        public string RequestId { get; set; } = string.Empty;

        public DateTime RunDateTime { get; set; }

        public string Status { get; set; } = "Unknown";

        public int CandidateCount { get; set; }

        public int ParetoSolutionCount { get; set; }

        public bool MatchesExhaustiveReference { get; set; }

        public string RequestFilePath { get; set; } = string.Empty;

        public string ResultFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable label displayed in the dashboard history list.
        /// </summary>
        public string DisplayLabel =>
            $"{RunDateTime:yyyy-MM-dd HH:mm} | " +
            $"{Status.ToUpperInvariant()} | " +
            $"{CandidateCount} candidates | " +
            $"{ParetoSolutionCount} Pareto solutions | " +
            $"{RequestId}";
    }
}