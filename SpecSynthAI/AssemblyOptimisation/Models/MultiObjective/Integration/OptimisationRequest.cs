using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration
{
    public sealed class OptimisationRequest
    {
        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; } = "1.0.0";

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("project")]
        public ProjectContext Project { get; set; } = new();

        [JsonPropertyName("baseline_assembly")]
        public BaselineAssemblyContext? BaselineAssembly { get; set; }

        [JsonPropertyName("optimisation")]
        public OptimisationSettings Optimisation { get; set; } = new();

        [JsonPropertyName("execution")]
        public ExecutionSettings Execution { get; set; } = new();
    }

    public sealed class ProjectContext
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Describes the actual Revit wall assembly used as the optimisation baseline.
    /// Existing fields are retained for compatibility with saved requests.
    /// </summary>
    public sealed class BaselineAssemblyContext
    {
        [JsonPropertyName("assembly_id")]
        public string AssemblyId { get; set; } = string.Empty;

        [JsonPropertyName("assembly_name")]
        public string AssemblyName { get; set; } = string.Empty;

        [JsonPropertyName("u_value_w_m2k")]
        public double? UValueWm2K { get; set; }

        [JsonPropertyName("total_thickness_mm")]
        public double? TotalThicknessMm { get; set; }

        [JsonPropertyName("layers")]
        public List<BaselineAssemblyLayer> Layers { get; set; } = new();
    }

    /// <summary>
    /// One material layer in the original Revit compound wall structure.
    /// Conductivity is nullable so missing source data is never fabricated.
    /// </summary>
    public sealed class BaselineAssemblyLayer
    {
        [JsonPropertyName("layer_order")]
        public int LayerOrder { get; set; }

        [JsonPropertyName("material_name")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("thickness_mm")]
        public double ThicknessMm { get; set; }

        [JsonPropertyName("thermal_conductivity_w_mk")]
        public double? ThermalConductivityWmK { get; set; }

        [JsonPropertyName("conductivity_source")]
        public string ConductivitySource { get; set; } = string.Empty;
    }

    public sealed class OptimisationSettings
    {
        [JsonPropertyName("objectives")]
        public List<string> Objectives { get; set; } = new()
        {
            "minimise_u_value",
            "minimise_carbon",
            "minimise_cost"
        };

        [JsonPropertyName("constraints")]
        public OptimisationConstraints Constraints { get; set; } = new();

        [JsonPropertyName("algorithm")]
        public AlgorithmSettings Algorithm { get; set; } = new();
    }

    public sealed class OptimisationConstraints
    {
        [JsonPropertyName("maximum_wall_thickness_mm")]
        public double MaximumWallThicknessMm { get; set; } = 300;

        [JsonPropertyName("minimum_insulation_thickness_mm")]
        public double MinimumInsulationThicknessMm { get; set; } = 40;

        [JsonPropertyName("maximum_insulation_thickness_mm")]
        public double MaximumInsulationThicknessMm { get; set; } = 200;

        [JsonPropertyName("thickness_step_mm")]
        public double ThicknessStepMm { get; set; } = 10;
    }

    public sealed class AlgorithmSettings
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "NSGA-II";

        [JsonPropertyName("population_size")]
        public int PopulationSize { get; set; } = 40;

        [JsonPropertyName("generations")]
        public int Generations { get; set; } = 60;

        [JsonPropertyName("random_seed")]
        public int RandomSeed { get; set; } = 42;
    }

    public sealed class ExecutionSettings
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "controlled_stage7_2";

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 600;
    }
}
