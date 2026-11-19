using System;
using System.Collections.Generic;
using System.IO;
using SpecSynthAI.AssemblyOptimisation.Models;
using SpecSynthAI.AssemblyOptimisation.Models.ML;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

/// <summary>
/// Runs ML dataset creation after the normal assembly generation/evaluation
/// pipeline has completed. This class does not retrain or recalculate walls.
/// </summary>
public sealed class AssemblyMLDatasetPipeline
{
    private readonly AssemblyMLDatasetGenerator _generator = new();
    private readonly AssemblyMLDatasetExporter _exporter = new();
    private readonly AssemblyMLDatasetValidator _validator = new();

    public AssemblyMLDatasetValidationReport Run(
        AssemblyGenerationReport generationReport,
        AssemblyEvaluationReport evaluationReport,
        string outputDirectory,
        out List<AssemblyMLRecord> records)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));

        Directory.CreateDirectory(outputDirectory);

        records = _generator.Generate(generationReport, evaluationReport);
        AssemblyMLDatasetValidationReport validation = _validator.Validate(records);

        _exporter.ExportCsv(records, Path.Combine(outputDirectory, "assembly_ml_dataset_v1.csv"));
        _exporter.ExportJson(records, Path.Combine(outputDirectory, "assembly_ml_dataset_v1.json"));

        return validation;
    }
}
