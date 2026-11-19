using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Microsoft.Win32;

using SpecSynthAI.AssemblyOptimisation.Services;

using System;
using System.IO;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class RunAssemblyPipelineTestCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // ============================================
            // STEP 1: SELECT PRODUCTS JSON
            // ============================================

            string productsFilePath = SelectJsonFile(
                "Select Global Products JSON",
                "Global_Products_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(productsFilePath))
                return Result.Cancelled;


            // ============================================
            // STEP 2: SELECT PRODUCT VARIANTS JSON
            // ============================================

            string variantsFilePath = SelectJsonFile(
                "Select Global Product Variants JSON",
                "Global_Product_Variants_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(variantsFilePath))
                return Result.Cancelled;


            // ============================================
            // STEP 3: SELECT PRODUCT SOURCES JSON
            // ============================================

            string sourcesFilePath = SelectJsonFile(
                "Select Global Product Sources JSON",
                "Global_Product_Sources_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(sourcesFilePath))
                return Result.Cancelled;


            // ============================================
            // STEP 4: VALIDATE INPUT FILES
            // ============================================

            ValidateFile(productsFilePath);
            ValidateFile(variantsFilePath);
            ValidateFile(sourcesFilePath);


            // ============================================
            // STEP 5: DEFINE OUTPUT DIRECTORY
            // ============================================

            string? datasetDirectory =
                Path.GetDirectoryName(productsFilePath);

            if (string.IsNullOrWhiteSpace(datasetDirectory))
            {
                throw new InvalidOperationException(
                    "Could not determine the dataset directory.");
            }

            DirectoryInfo? datasetRoot =
                Directory.GetParent(datasetDirectory);

            if (datasetRoot == null)
            {
                throw new InvalidOperationException(
                    "Could not determine the dataset root directory.");
            }

            string outputDirectory =
                Path.Combine(
                    datasetRoot.FullName,
                    "05_Research_Experiments");

            Directory.CreateDirectory(outputDirectory);

            string outputReportPath =
                Path.Combine(
                    outputDirectory,
                    "phase1_integration_test.json");


            // ============================================
            // STEP 6: INITIALISE PIPELINE
            // ============================================

            var pipeline =
                new AssemblyResearchPipelineService();


            // ============================================
            // STEP 7: EXECUTE V1.2.0 PIPELINE
            // ============================================

            var result = pipeline.Run(
                productsFilePath,
                variantsFilePath,
                sourcesFilePath,
                outputReportPath);


            // ============================================
            // STEP 8: VERIFY OUTPUT FILE
            // ============================================

            if (!File.Exists(outputReportPath))
            {
                throw new InvalidOperationException(
                    "Pipeline completed without creating the JSON report.");
            }


            // ============================================
            // STEP 9: VALIDATE RESULTS
            // ============================================

            bool integrationPassed =
                result.CandidatesReceived > 0 &&
                result.ValidCalculations == result.CandidatesReceived &&
                result.InvalidCalculations == 0 &&
                result.LowestUValue.HasValue &&
                double.IsFinite(result.LowestUValue.Value) &&
                result.LowestUValue.Value > 0;

            string integrationStatus =
                integrationPassed
                    ? "PASS"
                    : "REVIEW REQUIRED";


            // ============================================
            // STEP 10: PREPARE RESULT SUMMARY
            // ============================================

            string lowestUValue =
                result.LowestUValue.HasValue
                    ? result.LowestUValue.Value.ToString("F6")
                    : "Not available";

            string report =
                "SPECSYNTH AI\n" +
                "ASSEMBLY PIPELINE INTEGRATION TEST\n" +
                "========================================\n\n" +

                "INPUT DATA\n" +
                "----------------------------------------\n" +

                $"Dataset Version: {result.DatasetVersion}\n\n" +

                $"Products:\n{productsFilePath}\n\n" +

                $"Variants:\n{variantsFilePath}\n\n" +

                $"Sources:\n{sourcesFilePath}\n\n" +

                "ASSEMBLY EVALUATION\n" +
                "----------------------------------------\n" +

                $"Candidates Received: {result.CandidatesReceived}\n" +

                $"Valid Calculations: {result.ValidCalculations}\n" +

                $"Invalid Calculations: {result.InvalidCalculations}\n\n" +

                "BEST PERFORMING ASSEMBLY\n" +
                "----------------------------------------\n" +

                $"{result.BestPerformingAssembly ?? "Not available"}\n\n" +

                $"Lowest U-Value: {lowestUValue} W/(m²·K)\n\n" +

                "INTEGRATION TEST STATUS\n" +
                "----------------------------------------\n" +

                $"{integrationStatus}\n\n" +

                "OUTPUT REPORT\n" +
                "----------------------------------------\n" +

                $"{outputReportPath}";


            // ============================================
            // STEP 11: DISPLAY RESULTS
            // ============================================

            TaskDialog.Show(
                "SpecSynth AI - Integration Test",
                report);


            // ============================================
            // STEP 12: RETURN RESULT
            // ============================================

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.ToString();

            TaskDialog.Show(
                "SpecSynth AI - Integration Test Failed",
                ex.ToString());

            return Result.Failed;
        }
    }


    // =====================================================
    // JSON FILE SELECTION
    // =====================================================

    private static string SelectJsonFile(
        string title,
        string defaultFileName)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,

            Filter = "JSON files (*.json)|*.json",

            FileName = defaultFileName,

            CheckFileExists = true,

            CheckPathExists = true,

            Multiselect = false
        };

        bool? selected = dialog.ShowDialog();

        return selected == true
            ? dialog.FileName
            : string.Empty;
    }


    // =====================================================
    // FILE VALIDATION
    // =====================================================

    private static void ValidateFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "A dataset file path cannot be empty.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Dataset file was not found.",
                filePath);
        }
    }
}