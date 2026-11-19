using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;
using SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration;

namespace SpecSynthAI.Services
{
    public sealed class OptimisationExecutionService
    {
        private const string ProjectRoot =
            @"D:\02_LEARNING\02_Masters_IAAC_AI_Architecture\10-Term_01\03-Research Studio\SpecSynth_AI";

        private const string PythonExecutable =
            @"C:\Users\FawadSaleem\Anaconda3\envs\arch_ai\python.exe";

        private static readonly HashSet<string> SupportedObjectives =
            new(StringComparer.Ordinal)
            {
                "minimise_u_value",
                "minimise_carbon",
                "minimise_cost"
            };

        private readonly string _integrationDirectory;
        private readonly string _adapterPath;
        private readonly string _outputDirectory;

        private readonly PythonEngineService _pythonEngine;
        private readonly OptimisationJsonService _jsonService;

        public OptimisationExecutionService()
        {
            _integrationDirectory = Path.Combine(
                ProjectRoot,
                "05_Research_Experiments",
                "04_Integration");

            _adapterPath = Path.Combine(
                _integrationDirectory,
                "run_optimisation.py");

            _outputDirectory = Path.Combine(
                _integrationDirectory,
                "output");

            _pythonEngine = new PythonEngineService();
            _jsonService = new OptimisationJsonService();
        }

        // =====================================================
        // MAIN METHOD — USER-DEFINED OPTIMISATION REQUEST
        // =====================================================

        public async Task<OptimisationResult> RunAsync(
            OptimisationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // 1. Validate execution environment.

            ValidateEnvironment();

            // 2. Validate user-defined request.

            ValidateUserRequest(request);

            // Generate a unique ID for every execution.
            // This prevents previous runs from being overwritten.

            request.RequestId =
                "SSAI-OPT-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");

            // 3. Create unique JSON paths.

            string requestPath = Path.Combine(
                _outputDirectory,
                $"request_{request.RequestId}.json");

            string resultPath = Path.Combine(
                _outputDirectory,
                $"result_{request.RequestId}.json");

            // 4. Save the exact request.

            _jsonService.WriteRequest(
                requestPath,
                request);

            // 5. Execute Python engine.

            await _pythonEngine.ExecuteAsync(
                PythonExecutable,
                _adapterPath,
                requestPath,
                resultPath,
                _integrationDirectory,
                request.Execution.TimeoutSeconds,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 6. Read Python result.

            OptimisationResult result =
                _jsonService.ReadResult(resultPath);

            // 7. Validate result.

            ValidateResult(result, request.RequestId);

            return result;
        }

        // =====================================================
        // LEGACY COMPATIBILITY METHOD
        // =====================================================

        // Keeps the existing dashboard call compiling
        // until its user-defined input interface is connected.

        public Task<OptimisationResult> RunAsync()
        {
            var request = new OptimisationRequest
            {
                Project = new ProjectContext
                {
                    ProjectId = "SSAI-STAGE7-2-DEMO",
                    ProjectName = "SpecSynth AI Optimisation Demo"
                }
            };

            return RunAsync(request);
        }

        // =====================================================
        // ENVIRONMENT VALIDATION
        // =====================================================

        private void ValidateEnvironment()
        {
            if (!File.Exists(PythonExecutable))
            {
                throw new FileNotFoundException(
                    "Python executable not found.",
                    PythonExecutable);
            }

            if (!Directory.Exists(_integrationDirectory))
            {
                throw new DirectoryNotFoundException(
                    _integrationDirectory);
            }

            if (!File.Exists(_adapterPath))
            {
                throw new FileNotFoundException(
                    "Optimisation adapter not found.",
                    _adapterPath);
            }

            Directory.CreateDirectory(_outputDirectory);
        }

        // =====================================================
        // REQUEST VALIDATION
        // =====================================================

        private void ValidateUserRequest(
            OptimisationRequest request)
        {
            if (request.Project == null ||
                string.IsNullOrWhiteSpace(request.Project.ProjectName))
            {
                throw new InvalidDataException(
                    "Project name is required.");
            }

            if (request.Optimisation == null)
            {
                throw new InvalidDataException(
                    "Optimisation settings are required.");
            }

            if (request.Optimisation.Constraints == null)
            {
                throw new InvalidDataException(
                    "Optimisation constraints are required.");
            }

            if (request.Optimisation.Algorithm == null)
            {
                throw new InvalidDataException(
                    "Algorithm settings are required.");
            }

            if (request.Execution == null)
            {
                throw new InvalidDataException(
                    "Execution settings are required.");
            }

            // Validate selected objectives.

            var objectives = request.Optimisation.Objectives;

            if (objectives == null || objectives.Count == 0)
            {
                throw new InvalidDataException(
                    "Select at least one optimisation objective.");
            }

            if (objectives.Distinct().Count() != objectives.Count)
            {
                throw new InvalidDataException(
                    "Duplicate optimisation objectives are not allowed.");
            }

            foreach (string objective in objectives)
            {
                if (!SupportedObjectives.Contains(objective))
                {
                    throw new InvalidDataException(
                        $"Unsupported optimisation objective: {objective}");
                }
            }

            // Validate thickness constraints.

            var constraints = request.Optimisation.Constraints;

            if (constraints.MinimumInsulationThicknessMm <= 0)
            {
                throw new InvalidDataException(
                    "Minimum insulation thickness must be positive.");
            }

            if (constraints.MaximumInsulationThicknessMm <
                constraints.MinimumInsulationThicknessMm)
            {
                throw new InvalidDataException(
                    "Maximum insulation thickness cannot be less than minimum thickness.");
            }

            if (constraints.ThicknessStepMm <= 0)
            {
                throw new InvalidDataException(
                    "Thickness increment must be positive.");
            }

            if (constraints.MaximumWallThicknessMm <= 0)
            {
                throw new InvalidDataException(
                    "Maximum wall thickness must be positive.");
            }

            // Validate algorithm.

            var algorithm = request.Optimisation.Algorithm;

            if (algorithm.Name != "NSGA-II")
            {
                throw new InvalidDataException(
                    "Only NSGA-II is currently supported.");
            }

            if (algorithm.PopulationSize < 2)
            {
                throw new InvalidDataException(
                    "Population size must be at least 2.");
            }

            if (algorithm.Generations < 1)
            {
                throw new InvalidDataException(
                    "Generations must be at least 1.");
            }

            if (request.Execution.Mode != "controlled_stage7_2")
            {
                throw new InvalidDataException(
                    "Unsupported execution mode.");
            }

            if (request.Execution.TimeoutSeconds < 1)
            {
                throw new InvalidDataException(
                    "Execution timeout must be positive.");
            }

            // Run the existing JSON service validation.

            _jsonService.SerializeRequest(request);
        }

        // =====================================================
        // RESULT VALIDATION
        // =====================================================

        private static void ValidateResult(
            OptimisationResult result,
            string expectedRequestId)
        {
            if (result == null)
            {
                throw new InvalidDataException(
                    "Python returned an empty result.");
            }

            if (result.RequestId != expectedRequestId)
            {
                throw new InvalidDataException(
                    "Result request ID does not match the submitted request.");
            }

            if (result.Status != "success")
            {
                throw new InvalidOperationException(
                    "Python optimisation failed: " +
                    result.Message);
            }

            if (result.Summary == null)
            {
                throw new InvalidDataException(
                    "Optimisation summary is missing.");
            }

            if (result.Solutions == null ||
                result.Solutions.Count == 0)
            {
                throw new InvalidDataException(
                    "Python returned no Pareto solutions.");
            }

            if (result.Solutions.Count !=
                result.Summary.ParetoSolutionCount)
            {
                throw new InvalidDataException(
                    "Pareto solution count does not match the result summary.");
            }
        }
    }
}