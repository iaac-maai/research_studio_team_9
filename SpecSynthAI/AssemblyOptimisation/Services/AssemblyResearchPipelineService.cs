using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using SpecSynthAI.AssemblyOptimisation.Models;
using SpecSynthAI.AssemblyOptimisation.Services.ML;

namespace SpecSynthAI.AssemblyOptimisation.Services;

/// <summary>
/// Runs the product-to-assembly-to-ThermalEngine research workflow.
/// Supports v1.1 single-file and v1.2 three-file product datasets.
/// Also generates an ML-ready dataset from evaluated candidates.
/// </summary>
public sealed class AssemblyResearchPipelineService
{
    private readonly ProductDatabaseReader _databaseReader;
    private readonly WallAssemblyGenerator _assemblyGenerator;
    private readonly AssemblyEvaluationService _evaluationService;
    private readonly AssemblyMLDatasetPipeline _mlDatasetPipeline;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AssemblyResearchPipelineService()
    {
        _databaseReader = new ProductDatabaseReader();
        _assemblyGenerator = new WallAssemblyGenerator();
        _evaluationService = new AssemblyEvaluationService();
        _mlDatasetPipeline = new AssemblyMLDatasetPipeline();
    }

    // =========================================================
    // V1.1 - SINGLE-FILE DATASET
    // =========================================================

    public AssemblyEvaluationReport Run(
        string productDatabasePath,
        string outputReportPath)
    {
        ValidateFile(
            productDatabasePath,
            nameof(productDatabasePath),
            "Product database JSON was not found.");

        ProductDatabase database =
            _databaseReader.Load(productDatabasePath);

        return RunPipeline(database, outputReportPath);
    }

    // =========================================================
    // V1.2 - THREE-FILE DATASET
    // =========================================================

    public AssemblyEvaluationReport Run(
        string productsFilePath,
        string variantsFilePath,
        string sourcesFilePath,
        string outputReportPath)
    {
        ValidateFile(
            productsFilePath,
            nameof(productsFilePath),
            "Product dataset JSON was not found.");

        ValidateFile(
            variantsFilePath,
            nameof(variantsFilePath),
            "Product variants JSON was not found.");

        ValidateFile(
            sourcesFilePath,
            nameof(sourcesFilePath),
            "Product sources JSON was not found.");

        ProductDatabase database =
            _databaseReader.Load(
                productsFilePath,
                variantsFilePath,
                sourcesFilePath);

        return RunPipeline(database, outputReportPath);
    }

    // =========================================================
    // SHARED RESEARCH PIPELINE
    // =========================================================

    private AssemblyEvaluationReport RunPipeline(
        ProductDatabase database,
        string outputReportPath)
    {
        if (database is null)
        {
            throw new ArgumentNullException(nameof(database));
        }

        if (string.IsNullOrWhiteSpace(outputReportPath))
        {
            throw new ArgumentException(
                "Output report path cannot be empty.",
                nameof(outputReportPath));
        }

        // -----------------------------------------------------
        // STEP 1 — Generate assembly candidates
        // -----------------------------------------------------

        AssemblyGenerationReport generated =
            _assemblyGenerator.Generate(database);

        // -----------------------------------------------------
        // STEP 2 — Evaluate candidates using ThermalEngine
        // -----------------------------------------------------

        AssemblyEvaluationReport evaluated =
            _evaluationService.Evaluate(generated);

        // -----------------------------------------------------
        // STEP 3 — Create output directory
        // -----------------------------------------------------

        string fullReportPath =
            Path.GetFullPath(outputReportPath);

        string? outputDirectory =
            Path.GetDirectoryName(fullReportPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // -----------------------------------------------------
        // STEP 4 — Export evaluation report
        // -----------------------------------------------------

        string evaluationJson =
            JsonSerializer.Serialize(
                evaluated,
                _jsonOptions);

        File.WriteAllText(
            fullReportPath,
            evaluationJson);

        // -----------------------------------------------------
        // STEP 5 — Generate ML dataset
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            string mlOutputDirectory =
                Path.Combine(outputDirectory, "ML");

            var validation =
                _mlDatasetPipeline.Run(
                    generated,
                    evaluated,
                    mlOutputDirectory,
                    out var mlRecords);

            // -------------------------------------------------
            // STEP 6 — Export ML validation report
            // -------------------------------------------------

            string validationJson =
                JsonSerializer.Serialize(
                    validation,
                    _jsonOptions);

            File.WriteAllText(
                Path.Combine(
                    mlOutputDirectory,
                    "assembly_ml_validation_v1.json"),
                validationJson);

            // -------------------------------------------------
            // STEP 7 — Add ML generation summary
            // -------------------------------------------------

            evaluated.Warnings.Add(
                $"ML dataset generated: {mlRecords.Count} records.");

            evaluated.Warnings.Add(
                $"ML dataset validation: " +
                $"{validation.RecordsWithValidTarget} valid targets.");

            evaluated.Warnings.Add(
                "ML labels are ThermalEngine-generated calculations, " +
                "not measured building performance.");

            // Update evaluation report with the ML summary.
            evaluationJson =
                JsonSerializer.Serialize(
                    evaluated,
                    _jsonOptions);

            File.WriteAllText(
                fullReportPath,
                evaluationJson);
        }

        return evaluated;
    }

    // =========================================================
    // FILE VALIDATION
    // =========================================================

    private static void ValidateFile(
        string path,
        string parameterName,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                parameterName);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                errorMessage,
                path);
        }
    }
}