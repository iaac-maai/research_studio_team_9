using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Microsoft.Win32;

using SpecSynthAI.AssemblyOptimisation.Services;

using System;
using System.IO;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class RunAssemblyDatasetPipelineTestCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // ==========================================
            // STEP 1 — SELECT PRODUCT DATABASE
            // ==========================================

            var productDialog = new OpenFileDialog
            {
                Title = "Select Verified Product Database",
                Filter = "JSON files (*.json)|*.json",
                FileName = "specsynth_verified_open_products_v1.1.0.json",
                CheckFileExists = true,
                Multiselect = false
            };

            if (productDialog.ShowDialog() != true)
                return Result.Cancelled;

            string productDatabasePath = productDialog.FileName;

            // ==========================================
            // STEP 2 — SELECT ASSEMBLY DATASET
            // ==========================================

            var assemblyDialog = new OpenFileDialog
            {
                Title = "Select Wall Assembly Dataset",
                Filter = "JSON files (*.json)|*.json",
                FileName = "specsynth_wall_assemblies_dev_v1.1.0.json",
                CheckFileExists = true,
                Multiselect = false
            };

            if (assemblyDialog.ShowDialog() != true)
                return Result.Cancelled;

            string assemblyDatasetPath = assemblyDialog.FileName;

            // ==========================================
            // STEP 3 — PREPARE OUTPUT PATH
            // ==========================================

            string? datasetDirectory =
                Path.GetDirectoryName(productDatabasePath);

            if (string.IsNullOrWhiteSpace(datasetDirectory))
            {
                throw new InvalidOperationException(
                    "Could not determine product dataset directory.");
            }

            DirectoryInfo? projectDirectory =
                Directory.GetParent(datasetDirectory);

            if (projectDirectory == null)
            {
                throw new InvalidOperationException(
                    "Could not determine project directory.");
            }

            string outputDirectory = Path.Combine(
                projectDirectory.FullName,
                "05_Research_Experiments");

            Directory.CreateDirectory(outputDirectory);

            string outputReportPath = Path.Combine(
                outputDirectory,
                "assembly_dataset_evaluation_v1.1.0.json");

            // ==========================================
            // STEP 4 — RUN DATASET PIPELINE
            // ==========================================

            var pipeline = new AssemblyDatasetPipelineService();

            var result = pipeline.Run(
                productDatabasePath,
                assemblyDatasetPath,
                outputReportPath);

            // ==========================================
            // STEP 5 — DISPLAY RESULTS
            // ==========================================

            string lowestUValue = result.LowestUValue.HasValue
                ? result.LowestUValue.Value.ToString("F6")
                : "Not available";

            string report =
                "SPECSYNTH AI\n" +
                "ASSEMBLY DATASET PIPELINE TEST\n" +
                "========================================\n\n" +

                "DATASET INFORMATION\n" +
                "----------------------------------------\n" +
                $"Dataset ID: {result.DatasetId}\n" +
                $"Schema Version: {result.SchemaVersion}\n\n" +

                "ASSEMBLY MAPPING\n" +
                "----------------------------------------\n" +
                $"Assemblies Received: {result.AssembliesReceived}\n" +
                $"Ready for Calculation: {result.AssembliesReadyForCalculation}\n" +
                $"Not Ready: {result.AssembliesNotReadyForCalculation}\n\n" +

                "THERMAL EVALUATION\n" +
                "----------------------------------------\n" +
                $"Calculations Attempted: {result.CandidatesReceived}\n" +
                $"Valid Calculations: {result.ValidCalculations}\n" +
                $"Invalid Calculations: {result.InvalidCalculations}\n\n" +

                "BEST PERFORMING ASSEMBLY\n" +
                "----------------------------------------\n" +
                $"{result.BestPerformingAssembly ?? "Not available"}\n" +
                $"Lowest U-Value: {lowestUValue} W/(m²·K)\n\n" +

                "OUTPUT REPORT\n" +
                "----------------------------------------\n" +
                outputReportPath;

            TaskDialog.Show(
                "SpecSynth AI - Assembly Dataset Pipeline",
                report);

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.ToString();

            TaskDialog.Show(
                "SpecSynth AI - Dataset Pipeline Failed",
                ex.ToString());

            return Result.Failed;
        }
    }
}