using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpecSynthAI.AssemblyOptimisation.Services;

namespace SpecSynthAI.Commands;

[Transaction(TransactionMode.ReadOnly)]
public sealed class RunAssemblyDatasetPipelineCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            // Update this root only if your SpecSynth_AI project is stored elsewhere.
            const string projectRoot =
                @"D:\02_LEARNING\02_Masters_IAAC_AI_Architecture\10-Term_01\03-Research Studio\SpecSynth_AI";

            string datasetFolder = Path.Combine(
                projectRoot, "04_Product_Dataset", "02_Datasets");

            string productPath = Path.Combine(
                datasetFolder,
                "specsynth_verified_open_products_v1.1.0(5).json");

            string assemblyPath = Path.Combine(
                datasetFolder,
                "Development",
                "specsynth_wall_assemblies_dev_v1.1.0.json");

            string outputPath = Path.Combine(
                projectRoot,
                "04_Product_Dataset",
                "04_Pipeline",
                "assembly_evaluation_report_v1.1.0.json");

            var pipeline = new AssemblyDatasetPipelineService();
            var report = pipeline.Run(productPath, assemblyPath, outputPath);

            string rankedResults = string.Join(
                Environment.NewLine,
                report.DatasetAssemblies.Select(item =>
                    $"{item.AssemblyId}: " +
                    (item.ThermalResult.IsValid && item.ThermalResult.UValue.HasValue
                        ? $"U = {item.ThermalResult.UValue.Value:F4} W/(m²·K)"
                        : "Calculation invalid")));

            TaskDialog.Show(
                "SpecSynth AI — Assembly Evaluation",
                $"Pipeline completed.\n\n" +
                $"Assemblies received: {report.AssembliesReceived}\n" +
                $"Ready for calculation: {report.AssembliesReadyForCalculation}\n" +
                $"Not ready: {report.AssembliesNotReadyForCalculation}\n" +
                $"Valid thermal calculations: {report.ValidCalculations}\n" +
                $"Invalid thermal calculations: {report.InvalidCalculations}\n\n" +
                $"{rankedResults}\n\n" +
                $"Report saved to:\n{outputPath}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            TaskDialog.Show(
                "SpecSynth AI — Pipeline Error",
                ex.Message);
            return Result.Failed;
        }
    }
}
