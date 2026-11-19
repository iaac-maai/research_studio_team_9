using System;
using System.IO;
using System.Linq;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;
using SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration;

namespace SpecSynthAI.Diagnostics
{
    public static class OptimisationIntegrationValidationRunner
    {
        public static DiagnosticValidationCaseResult RunResultDeserializationTest(
            string resultJsonPath)
        {
            var test = new DiagnosticValidationCaseResult
            {
                TestName = "Stage 9.3 - Python optimisation JSON deserialization"
            };

            try
            {
                if (string.IsNullOrWhiteSpace(resultJsonPath))
                    throw new ArgumentException("Result JSON path is required.");

                if (!File.Exists(resultJsonPath))
                    throw new FileNotFoundException(
                        "Optimisation result JSON was not found.",
                        resultJsonPath);

                var jsonService = new OptimisationJsonService();

                OptimisationResult result =
                    jsonService.ReadResult(resultJsonPath);

                if (result.Status != "success")
                    throw new Exception(
                        $"Expected success status, received '{result.Status}'.");

                if (result.Solutions.Count != result.Summary.ParetoSolutionCount)
                    throw new Exception(
                        "Solution count does not match Pareto solution count.");

                if (result.Summary.ParetoSolutionCount != 18)
                    throw new Exception(
                        $"Expected 18 Pareto solutions, received " +
                        $"{result.Summary.ParetoSolutionCount}.");

                if (!result.Summary.MatchesExhaustiveReference)
                    throw new Exception(
                        "NSGA-II results do not match the exhaustive reference.");

                if (!result.Provenance.EngineInvoked)
                    throw new Exception(
                        "Python engine invocation was not confirmed.");

                if (!result.Solutions.Any(s => s.ModelOutOfDomain))
                    throw new Exception(
                        "Out-of-domain flags were not preserved.");

                var firstSolution = result.Solutions.First();

                if (string.IsNullOrWhiteSpace(firstSolution.MaterialId))
                    throw new Exception("Material ID is missing.");

                if (firstSolution.UValueWm2K <= 0)
                    throw new Exception("Invalid U-value received.");

                test.Passed = true;
                test.ActualCalculationStatus = "JSON DESERIALIZATION PASSED";
                test.ActualDataStatus = "INTEGRATION TEST PASSED";
                test.Message =
                    $"Successfully loaded {result.Solutions.Count} Pareto solutions. " +
                    $"Out-of-domain solutions: " +
                    $"{result.Solutions.Count(s => s.ModelOutOfDomain)}. " +
                    $"Model: {result.Provenance.Model}.";
            }
            catch (Exception ex)
            {
                test.Passed = false;
                test.ActualCalculationStatus = "TEST FAILED";
                test.ActualDataStatus = "REQUIRES REVIEW";
                test.Message = ex.Message;
            }

            return test;
        }
    }
}