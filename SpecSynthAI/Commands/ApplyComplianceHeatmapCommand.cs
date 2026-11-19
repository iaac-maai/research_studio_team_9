using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SpecSynthAI.Models;
using SpecSynthAI.Events;
using SpecSynthAI.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ApplyComplianceHeatmapCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            bool sessionStarted = false;

            try
            {
                UIDocument uiDoc =
                    commandData.Application.ActiveUIDocument;

                if (uiDoc == null)
                    return Result.Failed;

                Document doc = uiDoc.Document;
                View view = doc.ActiveView;

                if (view == null || view.IsTemplate)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "Open a model view before applying the heatmap.");

                    return Result.Failed;
                }

                // ==========================================
                // CHECK ACTIVE SESSION
                // ==========================================

                if (ComplianceHeatmapSession.IsActive)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "A Compliance Heatmap session is already active.\n\n" +
                        "Use Reset Compliance Heatmap before applying it again.");

                    return Result.Cancelled;
                }

                // ==========================================
                // CHECK RECOVERY RECORD
                // ==========================================

                if (PersistentHeatmapRecoveryStore.TryGetRecoveryRecord(
                    doc, out _))
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "An unfinished heatmap recovery record already exists.\n\n" +
                        "Restore the previous heatmap before applying a new one.");

                    return Result.Cancelled;
                }

                // ==========================================
                // COLLECT WALLS
                // ==========================================

                List<Wall> walls =
                    new FilteredElementCollector(doc, view.Id)
                        .OfClass(typeof(Wall))
                        .WhereElementIsNotElementType()
                        .Cast<Wall>()
                        .ToList();

                var results = new List<WallResult>();

                // ==========================================
                // INSPECT WALLS
                // ==========================================

                foreach (Wall wall in walls)
                {
                    WallInspectionData inspection =
                        ComplianceInspectionService.InspectWall(
                            doc,
                            wall.Id);

                    ComplianceVisualStatus status =
                        inspection.ComplianceStatus;

                    results.Add(
                        new WallResult(wall, status));

                    Debug.WriteLine(
                        "SPECSYNTH HEATMAP INSPECTION | " +
                        $"Wall ID: {wall.Id} | " +
                        $"Wall: {wall.Name} | " +
                        $"U-value: {inspection.WallUValue} | " +
                        $"Thermal status: {inspection.WallThermalStatus} | " +
                        $"Overall status: {inspection.ComplianceStatus} | " +
                        $"Explanation: {inspection.ComplianceExplanation}");
                }

                if (results.Count == 0)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "No walls were found in the active view.");

                    return Result.Succeeded;
                }

                // ==========================================
                // FIND SOLID FILL PATTERN
                // ==========================================

                FillPatternElement? solidFill =
                    new FilteredElementCollector(doc)
                        .OfClass(typeof(FillPatternElement))
                        .Cast<FillPatternElement>()
                        .FirstOrDefault(
                            x => x.GetFillPattern().IsSolidFill);

                // ==========================================
                // STATUS COUNTS
                // ==========================================

                int pass = 0;
                int fail = 0;
                int warning = 0;
                int cannotVerify = 0;

                // ==========================================
                // START HEATMAP SESSION
                // ==========================================

                ComplianceHeatmapSession.BeginSession(
                    doc,
                    view);

                sessionStarted = true;

                using (Transaction tx = new Transaction(
                    doc,
                    "SpecSynth AI - Apply Compliance Heatmap"))
                {
                    tx.Start();

                    // ======================================
                    // SAVE RECOVERY SNAPSHOT
                    // ======================================

                    PersistentHeatmapRecoveryStore.CreateRecoverySnapshot(
                        doc,
                        view,
                        results.Select(x => x.Wall.Id));

                    // ======================================
                    // APPLY GRAPHICS
                    // ======================================

                    foreach (WallResult item in results)
                    {
                        ComplianceHeatmapSession.SaveOriginalOverride(
                            view,
                            item.Wall.Id);

                        Color colour =
                            ColourFor(item.Status);

                        OverrideGraphicSettings ogs =
                            new OverrideGraphicSettings();

                        ogs.SetProjectionLineColor(colour);
                        ogs.SetCutLineColor(colour);
                        ogs.SetSurfaceTransparency(35);

                        if (solidFill != null)
                        {
                            ogs.SetSurfaceForegroundPatternId(
                                solidFill.Id);

                            ogs.SetSurfaceForegroundPatternColor(
                                colour);

                            ogs.SetSurfaceForegroundPatternVisible(true);

                            ogs.SetCutForegroundPatternId(
                                solidFill.Id);

                            ogs.SetCutForegroundPatternColor(
                                colour);

                            ogs.SetCutForegroundPatternVisible(true);
                        }

                        // Apply the graphics override.
                        view.SetElementOverrides(
                            item.Wall.Id,
                            ogs);

                        // ==================================
                        // VERIFY APPLIED GRAPHICS
                        // ==================================

                        OverrideGraphicSettings applied =
                            view.GetElementOverrides(
                                item.Wall.Id);

                        Color appliedColour =
                            applied.SurfaceForegroundPatternColor;

                        Debug.WriteLine(
                            "SPECSYNTH HEATMAP GRAPHICS | " +
                            $"Wall ID: {item.Wall.Id} | " +
                            $"Status: {item.Status} | " +
                            $"Expected RGB: " +
                            $"{colour.Red},{colour.Green},{colour.Blue} | " +
                            $"Applied RGB: " +
                            $"{appliedColour.Red}," +
                            $"{appliedColour.Green}," +
                            $"{appliedColour.Blue}");

                        // ==================================
                        // UPDATE COUNTS
                        // ==================================

                        switch (item.Status)
                        {
                            case ComplianceVisualStatus.Pass:
                                pass++;
                                break;

                            case ComplianceVisualStatus.Fail:
                                fail++;
                                break;

                            case ComplianceVisualStatus.Warning:
                                warning++;
                                break;

                            default:
                                cannotVerify++;
                                break;
                        }
                    }

                    tx.Commit();
                }

                // ==========================================
                // UPDATE COMPLIANCE PANE
                // ==========================================

                ComplianceModePaneProvider.UpdateState(
                    true,
                    pass,
                    fail,
                    warning,
                    cannotVerify,
                    view.Name);

                // ==========================================
                // START SELECTION MONITORING
                // ==========================================

                ComplianceSelectionHandler.Start(
                    commandData.Application);

                // ==========================================
                // COMPLETION MESSAGE
                // ==========================================

                TaskDialog.Show(
                    "SpecSynth AI - Spatial Compliance Heatmap",

                    "Compliance Heatmap Mode is now active.\n\n" +

                    $"Walls visualised: {results.Count}\n" +
                    $"PASS (Green): {pass}\n" +
                    $"FAIL (Red): {fail}\n" +
                    $"WARNING (Amber): {warning}\n" +
                    $"CANNOT VERIFY (Blue): {cannotVerify}\n\n" +

                    "Classification uses the overall wall inspection result.\n\n" +

                    "Select a heatmapped wall to inspect its properties.\n\n" +

                    "Use Reset Compliance Heatmap to restore the original graphics.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                if (sessionStarted)
                    ComplianceHeatmapSession.EndSession();

                message = ex.ToString();

                TaskDialog.Show(
                    "SpecSynth AI",
                    "Heatmap failed:\n\n" + ex.Message);

                return Result.Failed;
            }
        }

        // ==========================================
        // HEATMAP COLOUR MAPPING
        // ==========================================

        private static Color ColourFor(
            ComplianceVisualStatus status)
        {
            return status switch
            {
                ComplianceVisualStatus.Pass =>
                    new Color(46, 160, 90),

                ComplianceVisualStatus.Fail =>
                    new Color(220, 53, 69),

                ComplianceVisualStatus.Warning =>
                    new Color(245, 166, 35),

                ComplianceVisualStatus.Information =>
                    new Color(52, 120, 190),

                _ =>
                    new Color(150, 150, 150)
            };
        }

        // ==========================================
        // WALL RESULT MODEL
        // ==========================================

        private class WallResult
        {
            public Wall Wall { get; }

            public ComplianceVisualStatus Status { get; }

            public WallResult(
                Wall wall,
                ComplianceVisualStatus status)
            {
                Wall = wall;
                Status = status;
            }
        }
    }
}