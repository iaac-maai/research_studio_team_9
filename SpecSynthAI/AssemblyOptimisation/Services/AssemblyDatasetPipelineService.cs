using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using SpecSynthAI.AssemblyOptimisation.Models;

namespace SpecSynthAI.AssemblyOptimisation.Services;

/// <summary>
/// Runs the verified-product -> assembly-dataset -> ThermalEngine workflow
/// and writes the complete mapping and thermal evaluation report as JSON.
/// Supports both v1.1 single-file and v1.2 three-file product datasets.
/// </summary>
public sealed class AssemblyDatasetPipelineService
{
    private readonly ProductDatabaseReader _productDatabaseReader;
    private readonly WallAssemblyDatasetMapper _assemblyDatasetMapper;
    private readonly AssemblyEvaluationService _evaluationService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    public AssemblyDatasetPipelineService()
    {
        _productDatabaseReader = new ProductDatabaseReader();
        _assemblyDatasetMapper = new WallAssemblyDatasetMapper();
        _evaluationService = new AssemblyEvaluationService();
    }

    // =========================================================
    // V1.1 - SINGLE-FILE DATASET
    // Preserved for backward compatibility.
    // =========================================================

    public AssemblyEvaluationReport Run(
        string productDatabasePath,
        string assemblyDatasetPath,
        string outputReportPath)
    {
        ValidatePath(productDatabasePath, nameof(productDatabasePath));

        ValidateFileExists(
            productDatabasePath,
            "Verified product database JSON was not found.");

        ProductDatabase productDatabase =
            _productDatabaseReader.Load(productDatabasePath);

        return RunPipeline(
            productDatabase,
            assemblyDatasetPath,
            outputReportPath);
    }

    // =========================================================
    // V1.2 - THREE-FILE DATASET
    // =========================================================

    public AssemblyEvaluationReport Run(
        string productsFilePath,
        string variantsFilePath,
        string sourcesFilePath,
        string assemblyDatasetPath,
        string outputReportPath)
    {
        ValidatePath(productsFilePath, nameof(productsFilePath));
        ValidatePath(variantsFilePath, nameof(variantsFilePath));
        ValidatePath(sourcesFilePath, nameof(sourcesFilePath));

        ValidateFileExists(
            productsFilePath,
            "Product dataset JSON was not found.");

        ValidateFileExists(
            variantsFilePath,
            "Product variants JSON was not found.");

        ValidateFileExists(
            sourcesFilePath,
            "Product sources JSON was not found.");

        // 1. Load and combine the three v1.2 JSON files.
        ProductDatabase productDatabase =
            _productDatabaseReader.Load(
                productsFilePath,
                variantsFilePath,
                sourcesFilePath);

        // 2. Continue through the existing assembly pipeline.
        return RunPipeline(
            productDatabase,
            assemblyDatasetPath,
            outputReportPath);
    }

    // =========================================================
    // SHARED ASSEMBLY PIPELINE
    // =========================================================

    private AssemblyEvaluationReport RunPipeline(
        ProductDatabase productDatabase,
        string assemblyDatasetPath,
        string outputReportPath)
    {
        ValidatePath(
            assemblyDatasetPath,
            nameof(assemblyDatasetPath));

        ValidatePath(
            outputReportPath,
            nameof(outputReportPath));

        ValidateFileExists(
            assemblyDatasetPath,
            "Wall assembly dataset JSON was not found.");

        // 1. Resolve assembly product/variant references
        // and validate supported layers.
        AssemblyDatasetMappingReport mappingReport =
            _assemblyDatasetMapper.LoadAndMap(
                assemblyDatasetPath,
                productDatabase);

        // 2. Delegate thermal calculations and ranking
        // to the existing evaluation service.
        AssemblyEvaluationReport evaluationReport =
            _evaluationService.Evaluate(mappingReport);

        // 3. Create output directory if necessary.
        string? outputDirectory =
            Path.GetDirectoryName(
                Path.GetFullPath(outputReportPath));

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // 4. Export evaluation report.
        string reportJson = JsonSerializer.Serialize(
            evaluationReport,
            _jsonOptions);

        File.WriteAllText(
            outputReportPath,
            reportJson);

        return evaluationReport;
    }

    // =========================================================
    // VALIDATION HELPERS
    // =========================================================

    private static void ValidatePath(
        string path,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateFileExists(
        string path,
        string errorMessage)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                errorMessage,
                path);
        }
    }
}