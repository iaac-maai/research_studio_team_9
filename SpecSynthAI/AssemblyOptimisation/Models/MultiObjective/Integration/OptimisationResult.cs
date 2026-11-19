using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration
{
    public sealed class OptimisationResult
    {
        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; } = "1.0.0";

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "failed";

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("summary")]
        public OptimisationSummary Summary { get; set; } = new();

        [JsonPropertyName("solutions")]
        public List<OptimisationSolution> Solutions { get; set; } = new();

        [JsonPropertyName("provenance")]
        public OptimisationProvenance Provenance { get; set; } = new();
    }

    public sealed class OptimisationSummary
    {
        [JsonPropertyName("candidate_count")]
        public int CandidateCount { get; set; }

        [JsonPropertyName("pareto_solution_count")]
        public int ParetoSolutionCount { get; set; }

        [JsonPropertyName("exhaustive_reference_pareto_count")]
        public int ExhaustiveReferenceParetoCount { get; set; }

        [JsonPropertyName("matches_exhaustive_reference")]
        public bool MatchesExhaustiveReference { get; set; }
    }

    public sealed class OptimisationSolution
    {
        [JsonPropertyName("material_id")]
        public string MaterialId { get; set; } = string.Empty;

        [JsonPropertyName("material_label")]
        public string MaterialLabel { get; set; } = string.Empty;

        [JsonPropertyName("thickness_mm")]
        public double ThicknessMm { get; set; }

        [JsonPropertyName("u_value_W_m2K")]
        public double UValueWm2K { get; set; }

        [JsonPropertyName("carbon_kgCO2e_m2")]
        public double? CarbonKgCo2eM2 { get; set; }

        [JsonPropertyName("cost_eur_m2")]
        public double? CostEurM2 { get; set; }

        [JsonPropertyName("wall_thickness_mm")]
        public double WallThicknessMm { get; set; }

        [JsonPropertyName("feasible")]
        public bool Feasible { get; set; }

        [JsonPropertyName("model_out_of_domain")]
        public bool ModelOutOfDomain { get; set; }

        [JsonPropertyName("out_of_domain_features")]
        public string? OutOfDomainFeatures { get; set; }

        [JsonPropertyName("substrate_resistance_m2k_w")]
        public double? SubstrateResistanceM2KW { get; set; }

        [JsonPropertyName("insulation_lambda_w_mk")]
        public double? InsulationLambdaWmK { get; set; }

        [JsonPropertyName("lining_thickness_mm")]
        public double? LiningThicknessMm { get; set; }

        [JsonPropertyName("lining_lambda_w_mk")]
        public double? LiningLambdaWmK { get; set; }

        [JsonPropertyName("carbon_cost_status")]
        public string? CarbonCostStatus { get; set; }
    }

    public sealed class OptimisationProvenance
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("optimisation_algorithm")]
        public string? OptimisationAlgorithm { get; set; }

        [JsonPropertyName("training_data_scope")]
        public string? TrainingDataScope { get; set; }

        [JsonPropertyName("carbon_cost_data_status")]
        public string? CarbonCostDataStatus { get; set; }

        [JsonPropertyName("execution_mode")]
        public string? ExecutionMode { get; set; }

        [JsonPropertyName("engine_invoked")]
        public bool EngineInvoked { get; set; }

        [JsonPropertyName("stage7_2_report")]
        public JsonElement Stage7_2Report { get; set; }
    }
}
