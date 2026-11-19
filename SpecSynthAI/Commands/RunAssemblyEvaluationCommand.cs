using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Microsoft.Win32;

using SpecSynthAI.AssemblyOptimisation.Services;

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public sealed class RunAssemblyEvaluationCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // STEP 1: Select the v1.2.0 products database.
            string productsFilePath = SelectJsonFile(
                "Select Global Products Database",
                "Global_Products_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(productsFilePath))
                return Result.Cancelled;

            // STEP 2: Select the v1.2.0 variants database.
            string variantsFilePath = SelectJsonFile(
                "Select Global Product Variants Database",
                "Global_Product_Variants_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(variantsFilePath))
                return Result.Cancelled;

            // STEP 3: Select the v1.2.0 sources database.
            string sourcesFilePath = SelectJsonFile(
                "Select Global Sources Database",
                "Global_Sources_v1.2.0.json");

            if (string.IsNullOrWhiteSpace(sourcesFilePath))
                return Result.Cancelled;

            // STEP 4: Select where the evaluation report will be saved.
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Assembly Evaluation Report",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "phase1_integration_test.json"
            };

            if (saveDialog.ShowDialog() != true)
                return Result.Cancelled;

            string outputReportPath = saveDialog.FileName;

            // STEP 5: Run the v1.2.0 assembly research pipeline.
            var pipeline = new AssemblyResearchPipelineService();

            var report = pipeline.Run(
                productsFilePath,
                variantsFilePath,
                sourcesFilePath,
                outputReportPath);

            // STEP 6: Prepare the evaluation summary.
            var summary = new StringBuilder();

            summary.AppendLine("ASSEMBLY EVALUATION COMPLETED");
            summary.AppendLine("--------------------------------");
            summary.AppendLine("Database version: v1.2.0");

            summary.AppendLine(
                $"Products database: {Path.GetFileName(productsFilePath)}");

            summary.AppendLine(
                $"Variants database: {Path.GetFileName(variantsFilePath)}");

            summary.AppendLine(
                $"Sources database: {Path.GetFileName(sourcesFilePath)}");

            summary.AppendLine();

            summary.AppendLine($"Dataset version: {report.DatasetVersion}");
            summary.AppendLine($"Candidates received: {report.CandidatesReceived}");
            summary.AppendLine($"Valid calculations: {report.ValidCalculations}");
            summary.AppendLine($"Invalid calculations: {report.InvalidCalculations}");

            summary.AppendLine();
            summary.AppendLine("BEST THERMAL CANDIDATE");
            summary.AppendLine("--------------------------------");

            summary.AppendLine(
                report.BestPerformingAssembly ?? "No valid candidate");

            summary.AppendLine(
                report.LowestUValue.HasValue
                    ? $"U-value: {report.LowestUValue.Value:F4} W/(m²·K)"
                    : "U-value: unavailable");

            summary.AppendLine();
            summary.AppendLine("RANKED CANDIDATES");
            summary.AppendLine("--------------------------------");

            foreach (var item in report.RankedCandidates.Take(10))
            {
                string uValue = item.ThermalResult.UValue.HasValue
                    ? $"{item.ThermalResult.UValue.Value:F4} W/(m²·K)"
                    : "N/A";

                summary.AppendLine(
                    $"{item.AssemblyName} | {uValue} | " +
                    $"{(item.ThermalResult.IsValid ? "VALID" : "INVALID")}");
            }

            summary.AppendLine();
            summary.AppendLine("EVALUATION REPORT");
            summary.AppendLine(outputReportPath);

            summary.AppendLine();
            summary.AppendLine("ML DATASET OUTPUT");
            summary.AppendLine(
                Path.Combine(
                    Path.GetDirectoryName(outputReportPath) ?? "",
                    "ML"));

            summary.AppendLine();
            summary.AppendLine(
                "IMPORTANT: Ranking is thermal-only. It does not establish " +
                "complete system compatibility, regulatory compliance, " +
                "moisture safety, structural adequacy or fire safety.");

            TaskDialog.Show(
                "SpecSynth AI - Assembly Evaluation",
                summary.ToString());

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.ToString();

            TaskDialog.Show(
                "SpecSynth AI - Assembly Evaluation Error",
                "The assembly evaluation could not be completed:\n\n" +
                ex.Message);

            return Result.Failed;
        }
    }

    // Opens a JSON file selection dialog.
    private static string SelectJsonFile(
        string title,
        string defaultFileName)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "JSON files (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = false,
            FileName = defaultFileName
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : string.Empty;
    }
}