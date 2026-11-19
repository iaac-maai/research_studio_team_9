using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration
{
    public sealed class OptimisationJsonService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public string SerializeRequest(OptimisationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRequest(request);
            return JsonSerializer.Serialize(request, Options);
        }

        public OptimisationRequest DeserializeRequest(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Request JSON is empty.", nameof(json));
            var value = JsonSerializer.Deserialize<OptimisationRequest>(json, Options)
                        ?? throw new InvalidDataException("Could not deserialize optimisation request.");
            ValidateRequest(value);
            return value;
        }

        public OptimisationResult DeserializeResult(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Result JSON is empty.", nameof(json));
            var value = JsonSerializer.Deserialize<OptimisationResult>(json, Options)
                        ?? throw new InvalidDataException("Could not deserialize optimisation result.");
            if (value.SchemaVersion != "1.0.0") throw new InvalidDataException($"Unsupported result schema: {value.SchemaVersion}");
            return value;
        }

        public void WriteRequest(string path, OptimisationRequest request)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, SerializeRequest(request));
        }

        public OptimisationResult ReadResult(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Optimisation result not found.", path);
            return DeserializeResult(File.ReadAllText(path));
        }

        private static void ValidateRequest(OptimisationRequest request)
        {
            if (request.SchemaVersion != "1.0.0") throw new InvalidDataException("Unsupported request schema version.");
            if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("request_id is required.");
            if (request.Project == null || string.IsNullOrWhiteSpace(request.Project.ProjectName))
                throw new InvalidDataException("project.project_name is required.");
            if (request.Optimisation?.Constraints == null || request.Optimisation.Algorithm == null)
                throw new InvalidDataException("Optimisation constraints and algorithm are required.");

            var c = request.Optimisation.Constraints;
            if (c.MinimumInsulationThicknessMm <= 0 || c.MaximumInsulationThicknessMm < c.MinimumInsulationThicknessMm ||
                c.ThicknessStepMm <= 0 || c.MaximumWallThicknessMm <= 0)
                throw new InvalidDataException("Invalid thickness constraints.");

            if (request.Optimisation.Algorithm.PopulationSize < 2 || request.Optimisation.Algorithm.Generations < 1)
                throw new InvalidDataException("Invalid NSGA-II population or generation count.");

            if (request.Execution.Mode != "controlled_stage7_2")
                throw new InvalidDataException("Only 'controlled_stage7_2' execution mode is currently supported.");
            if (request.Execution.TimeoutSeconds < 1) throw new InvalidDataException("Timeout must be positive.");
        }
    }
}
