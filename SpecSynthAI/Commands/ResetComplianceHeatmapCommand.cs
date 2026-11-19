using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpecSynthAI.Models;
using System;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ResetComplianceHeatmapCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIDocument uiDoc =
                    commandData.Application.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "No active Revit document is available.");

                    return Result.Failed;
                }

                Document doc = uiDoc.Document;

                // Find persistent recovery data first.
                bool hasPersistentRecord =
                    PersistentHeatmapRecoveryStore.TryGetRecoveryRecord(
                        doc,
                        out PersistentHeatmapRecoveryStore.RecoveryRecord? record);

                // Legacy fallback for sessions created by the previous version.
                if (!hasPersistentRecord &&
                    !ComplianceHeatmapSession.IsActive)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "No active or recoverable Compliance Heatmap session was found.");

                    return Result.Succeeded;
                }

                int restoredCount = 0;
                int skippedCount = 0;

                using (Transaction transaction =
                    new Transaction(
                        doc,
                        "Reset SpecSynth Compliance Heatmap"))
                {
                    transaction.Start();

                    if (hasPersistentRecord && record != null)
                    {
                        View? originalView =
                            doc.GetElement(record.OriginalViewId) as View;

                        View? recoveryView =
                            doc.GetElement(record.RecoveryViewId) as View;

                        if (originalView == null || recoveryView == null)
                        {
                            throw new InvalidOperationException(
                                "The original view or recovery view could not be found. " +
                                "Recovery data has been preserved.");
                        }

                        // Restore every element from its original settings.
                        foreach (ElementId elementId in record.ElementIds)
                        {
                            Element? element = doc.GetElement(elementId);

                            if (element == null)
                            {
                                skippedCount++;
                                continue;
                            }

                            OverrideGraphicSettings originalSettings =
                                recoveryView.GetElementOverrides(elementId);

                            originalView.SetElementOverrides(
                                elementId,
                                originalSettings);

                            restoredCount++;
                        }

                        // Keep recovery data if any elements could not be restored.
                        if (skippedCount == 0)
                        {
                            PersistentHeatmapRecoveryStore
                                .DeleteRecoverySnapshot(doc, record);
                        }
                    }
                    else
                    {
                        // Legacy in-memory recovery.
                        ElementId? savedViewId =
                            ComplianceHeatmapSession.ActiveViewId;

                        if (savedViewId == null)
                        {
                            throw new InvalidOperationException(
                                "The legacy session does not contain a saved view.");
                        }

                        View? view =
                            doc.GetElement(savedViewId) as View;

                        if (view == null)
                        {
                            throw new InvalidOperationException(
                                "The original heatmap view could not be found.");
                        }

                        foreach (ElementId elementId in
                            ComplianceHeatmapSession.GetModifiedElementIds())
                        {
                            if (doc.GetElement(elementId) == null)
                            {
                                skippedCount++;
                                continue;
                            }

                            if (ComplianceHeatmapSession.TryGetOriginalOverride(
                                elementId,
                                out OverrideGraphicSettings? originalSettings)
                                && originalSettings != null)
                            {
                                view.SetElementOverrides(
                                    elementId,
                                    originalSettings);

                                restoredCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }

                    transaction.Commit();
                }

                // Update recovery indicator after successful transaction.
                ComplianceModePaneProvider.UpdateRecoveryState(
                    hasPersistentRecord && skippedCount > 0);

                // End the in-memory session only after successful restoration.
                if (skippedCount == 0)
                {
                    ComplianceHeatmapSession.EndSession();

                    ComplianceModePaneProvider.UpdateState(false);
                }

                TaskDialog.Show(
                    "SpecSynth AI",
                    "Compliance Heatmap reset operation completed.\n\n" +
                    $"Elements restored: {restoredCount}\n" +
                    $"Elements skipped: {skippedCount}\n\n" +
                    (skippedCount == 0
                        ? "Original graphics restored and recovery data removed."
                        : "Some elements could not be restored. " +
                          "Recovery data has been preserved for another attempt."));

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "SpecSynth AI",
                    "Could not reset the Compliance Heatmap:\n\n" +
                    ex.Message);

                return Result.Failed;
            }
        }
    }
}