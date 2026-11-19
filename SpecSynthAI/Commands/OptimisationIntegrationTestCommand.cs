using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Microsoft.Win32;

using SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration;

using System;
using System.IO;
using System.Linq;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class OptimisationIntegrationTestCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // 1. Select Python result JSON
                var dialog = new OpenFileDialog
                {
                    Title = "Select SpecSynth AI Optimisation Result",
                    Filter = "JSON files (*.json)|*.json",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true)
                    return Result.Cancelled;

                // 2. Read JSON through existing service
                var jsonService = new OptimisationJsonService();

                var result = jsonService.ReadResult(dialog.FileName);

                // 3. Validate optimisation result
                if (result.Status != "success")
                    throw new Exception("Python optimisation was not successful.");

                if (result.Solutions.Count !=
                    result.Summary.ParetoSolutionCount)
                {
                    throw new Exception("Pareto solution count mismatch.");
                }

                if (result.Summary.ParetoSolutionCount != 18)
                    throw new Exception("Expected 18 Pareto solutions.");

                if (!result.Summary.MatchesExhaustiveReference)
                    throw new Exception("Exhaustive reference comparison failed.");

                if (!result.Provenance.EngineInvoked)
                    throw new Exception("Python engine invocation not confirmed.");

                int outOfDomainCount =
                    result.Solutions.Count(s => s.ModelOutOfDomain);

                // 4. Display test results
                TaskDialog.Show(
                    "SpecSynth AI - Stage 9.3",
                    "JSON DESERIALIZATION SUCCESSFUL\n\n" +
                    $"Request ID: {result.RequestId}\n" +
                    $"Status: {result.Status}\n" +
                    $"Candidates: {result.Summary.CandidateCount}\n" +
                    $"Pareto solutions: {result.Solutions.Count}\n" +
                    $"Out-of-domain solutions: {outOfDomainCount}\n" +
                    $"Model: {result.Provenance.Model}\n\n" +
                    "Exhaustive reference match: YES\n\n" +
                    "Stage 9.3 integration test passed."
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "SpecSynth AI - Integration Test Failed",
                    ex.Message
                );

                return Result.Failed;
            }
        }
    }
}