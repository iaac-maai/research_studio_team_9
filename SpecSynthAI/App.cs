using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;

using SpecSynthAI.Models;

using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace SpecSynthAI
{
    public class App : IExternalApplication
    {
        // ============================================================
        // STARTUP
        // ============================================================

        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "SpecSynth AI";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Tab may already exist.
            }

            // Register document-opened event.
            application.ControlledApplication.DocumentOpened +=
                OnDocumentOpened;

            // Register Compliance Mode dockable pane.
            application.RegisterDockablePane(
                ComplianceModePaneProvider.PaneId,
                "SpecSynth AI",
                new ComplianceModePaneProvider()
            );

            // Create ribbon panel.
            RibbonPanel panel = application.CreateRibbonPanel(
                tabName,
                "Inspection"
            );

            string assemblyPath =
                Assembly.GetExecutingAssembly().Location;

            string iconDirectory =
                Path.GetDirectoryName(assemblyPath)!;

            // ========================================================
            // 1. INSPECT ELEMENT
            // ========================================================

            PushButtonData inspectButtonData = new PushButtonData(
                "InspectElement",
                "Inspect\nElement",
                assemblyPath,
                "SpecSynthAI.Commands.InspectElementCommand"
            );

            AddButton(
                panel,
                inspectButtonData,
                iconDirectory,
                "InspectElement.png"
            );

            // ========================================================
            // 2. EXPORT THERMAL SCHEDULE
            // ========================================================

            PushButtonData exportButtonData = new PushButtonData(
                "ExportThermalSchedule",
                "Export Thermal\nSchedule",
                assemblyPath,
                "SpecSynthAI.Commands.ExportThermalScheduleCommand"
            );

            AddButton(
                panel,
                exportButtonData,
                iconDirectory,
                "ExportThermalSchedule.png"
            );

            // ========================================================
            // 2.5 THERMAL EXTRACTION TEST
            // ========================================================

            PushButtonData thermalExtractionButtonData =
                new PushButtonData(
                    "ThermalExtractionTest",
                    "Thermal\nExtraction",
                    assemblyPath,
                    "SpecSynthAI.Commands.TestThermalExtractionCommand"
                );

            panel.AddItem(thermalExtractionButtonData);

            // ========================================================
            // 2.6 WALL PERFORMANCE
            // ========================================================

            PushButtonData wallPerformanceButtonData =
                new PushButtonData(
                    "CalculateWallPerformance",
                    "Wall\nPerformance",
                    assemblyPath,
                    "SpecSynthAI.Commands.CalculateWallPerformanceCommand"
                );

            panel.AddItem(wallPerformanceButtonData);

            // ========================================================
            // 3. ASSEMBLY EVALUATION
            // ========================================================

            PushButtonData assemblyEvaluationButtonData =
                new PushButtonData(
                    "RunAssemblyEvaluation",
                    "Assembly\nEvaluation",
                    assemblyPath,
                    "SpecSynthAI.Commands.RunAssemblyEvaluationCommand"
                );

            panel.AddItem(assemblyEvaluationButtonData);

            // ========================================================
            // 4. ASSEMBLY PIPELINE INTEGRATION TEST
            // ========================================================

            PushButtonData pipelineTestButtonData =
                new PushButtonData(
                    "RunAssemblyPipelineTest",
                    "Pipeline\nIntegration Test",
                    assemblyPath,
                    "SpecSynthAI.Commands.RunAssemblyPipelineTestCommand"
                );

            panel.AddItem(pipelineTestButtonData);

            // ========================================================
            // 5. ASSEMBLY DATASET PIPELINE TEST
            // ========================================================

            PushButtonData assemblyDatasetPipelineButtonData =
                new PushButtonData(
                    "RunAssemblyDatasetPipeline",
                    "Assembly Dataset\nPipeline",
                    assemblyPath,
                    "SpecSynthAI.Commands.RunAssemblyDatasetPipelineTestCommand"
                );

            panel.AddItem(assemblyDatasetPipelineButtonData);

            // ========================================================
            // 6. EXPORT PROJECT WALL PERFORMANCE
            // ========================================================

            PushButtonData projectWallExportButtonData =
                new PushButtonData(
                    "ExportProjectWallAssemblies",
                    "Export Project\nWalls",
                    assemblyPath,
                    "SpecSynthAI.Commands.ExportProjectWallAssembliesCommand"
                );

            panel.AddItem(projectWallExportButtonData);

            // ========================================================
            // 7. PROJECT REQUIREMENTS
            // ========================================================

            PushButtonData requirementsButtonData = new PushButtonData(
                "ProjectRequirements",
                "Project\nRequirements",
                assemblyPath,
                "SpecSynthAI.Commands.OpenRequirementsDialogCommand"
            );

            panel.AddItem(requirementsButtonData);

            // ========================================================
            // 8. COMPLIANCE MODE
            // ========================================================

            PushButtonData complianceModeButtonData = new PushButtonData(
                "ToggleComplianceMode",
                "Compliance\nMode",
                assemblyPath,
                "SpecSynthAI.Commands.ToggleComplianceModeCommand"
            );

            panel.AddItem(complianceModeButtonData);

            // ========================================================
            // 9. PERFORMANCE DASHBOARD
            // ========================================================

            PushButtonData performanceDashboardButtonData =
                new PushButtonData(
                    "PerformanceDashboard",
                    "Performance\nDashboard",
                    assemblyPath,
                    "SpecSynthAI.Commands.OpenPerformanceDashboardCommand"
                );

            panel.AddItem(performanceDashboardButtonData);

            // ========================================================
            // 10. MATERIAL IMPORTER
            // ========================================================

            PushButtonData materialImporterButtonData =
                new PushButtonData(
                    "MaterialImporter",
                    "Material\nImporter",
                    assemblyPath,
                    "SpecSynthAI.Commands.MaterialImporterCommand"
                );

            panel.AddItem(materialImporterButtonData);

            // ========================================================
            // 11. PROJECT THERMAL CLASSIFICATION VALIDATION
            // ========================================================

            PushButtonData classificationTestButtonData =
                new PushButtonData(
                    "ThermalClassificationValidation",
                    "Validate\nClassification",
                    assemblyPath,
                    "SpecSynthAI.Commands.ProjectThermalClassificationValidationCommand"
                );

            panel.AddItem(classificationTestButtonData);

            // ========================================================
            // 12. OPTIMISATION INTEGRATION TEST
            // ========================================================

            PushButtonData optimisationTestButtonData =
                new PushButtonData(
                    "OptimisationIntegrationTest",
                    "Test\nOptimisation",
                    assemblyPath,
                    "SpecSynthAI.Commands.OptimisationIntegrationTestCommand"
                );

            panel.AddItem(optimisationTestButtonData);

            return Result.Succeeded;
        }

        // ============================================================
        // AUTOMATIC HEATMAP RECOVERY
        // ============================================================

        private void OnDocumentOpened(
            object sender,
            DocumentOpenedEventArgs args)
        {
            Document doc = args.Document;

            bool recoveryRecordFound = false;

            try
            {
                if (doc == null || !doc.IsValidObject)
                    return;

                bool found =
                    PersistentHeatmapRecoveryStore.TryGetRecoveryRecord(
                        doc,
                        out PersistentHeatmapRecoveryStore.RecoveryRecord? record
                    );

                recoveryRecordFound = found && record != null;

                ComplianceModePaneProvider.UpdateRecoveryState(
                    recoveryRecordFound
                );

                if (!recoveryRecordFound || record == null)
                    return;

                // ----------------------------------------------------
                // READ-ONLY DOCUMENT CHECK
                // ----------------------------------------------------

                if (doc.IsReadOnly)
                {
                    TaskDialog.Show(
                        "SpecSynth AI - Automatic Recovery",
                        "A previous Compliance Heatmap session was detected, " +
                        "but this model is read-only.\n\n" +
                        "Recovery information has been preserved."
                    );

                    return;
                }

                int restoredCount = 0;
                int skippedCount = 0;

                // ----------------------------------------------------
                // RESTORE GRAPHICS
                // ----------------------------------------------------

                using (Transaction transaction = new Transaction(
                    doc,
                    "SpecSynth AI - Automatic Heatmap Recovery"))
                {
                    transaction.Start();

                    View? originalView =
                        doc.GetElement(record.OriginalViewId) as View;

                    View? recoveryView =
                        doc.GetElement(record.RecoveryViewId) as View;

                    if (originalView == null || recoveryView == null)
                    {
                        throw new InvalidOperationException(
                            "The original view or recovery view could not be found. " +
                            "Recovery information has been preserved."
                        );
                    }

                    foreach (ElementId elementId in record.ElementIds)
                    {
                        if (doc.GetElement(elementId) == null)
                        {
                            skippedCount++;
                            continue;
                        }

                        OverrideGraphicSettings originalSettings =
                            recoveryView.GetElementOverrides(elementId);

                        originalView.SetElementOverrides(
                            elementId,
                            originalSettings
                        );

                        restoredCount++;
                    }

                    // Delete recovery snapshot only when all elements
                    // have been successfully restored.

                    if (skippedCount == 0)
                    {
                        PersistentHeatmapRecoveryStore
                            .DeleteRecoverySnapshot(doc, record);
                    }

                    transaction.Commit();
                }

                // ----------------------------------------------------
                // UPDATE RECOVERY STATE
                // ----------------------------------------------------

                bool recoveryStillExists = skippedCount > 0;

                ComplianceModePaneProvider.UpdateRecoveryState(
                    recoveryStillExists
                );

                if (skippedCount == 0)
                {
                    ComplianceModePaneProvider.UpdateState(false);
                }

                // ----------------------------------------------------
                // RECOVERY RESULT
                // ----------------------------------------------------

                TaskDialog.Show(
                    "SpecSynth AI - Automatic Recovery",

                    "A previous Compliance Heatmap session was detected.\n\n" +
                    $"Elements restored: {restoredCount}\n" +
                    $"Elements skipped: {skippedCount}\n\n" +

                    (skippedCount == 0
                        ? "Original graphics have been restored successfully."
                        : "Some elements could not be restored. " +
                          "Recovery information has been preserved.")
                );
            }
            catch (Exception ex)
            {
                if (recoveryRecordFound)
                {
                    ComplianceModePaneProvider.UpdateRecoveryState(true);
                }

                TaskDialog.Show(
                    "SpecSynth AI - Recovery Error",
                    "Automatic heatmap recovery could not be completed.\n\n" +
                    ex.Message
                );
            }
        }

        // ============================================================
        // RIBBON BUTTON HELPER
        // ============================================================

        private static void AddButton(
            RibbonPanel panel,
            PushButtonData buttonData,
            string iconDirectory,
            string iconFileName)
        {
            string iconPath =
                Path.Combine(iconDirectory, iconFileName);

            if (File.Exists(iconPath))
            {
                buttonData.LargeImage = LoadRibbonIcon(iconPath);
            }

            panel.AddItem(buttonData);
        }

        // ============================================================
        // RIBBON ICON LOADER
        // ============================================================

        private static BitmapImage LoadRibbonIcon(string imagePath)
        {
            BitmapImage image = new BitmapImage();

            image.BeginInit();
            image.UriSource = new Uri(imagePath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            return image;
        }

        // ============================================================
        // SHUTDOWN
        // ============================================================

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened -=
                OnDocumentOpened;

            return Result.Succeeded;
        }
    }
}