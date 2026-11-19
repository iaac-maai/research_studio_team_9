using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class OpenPerformanceDashboardCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApplication =
                    commandData.Application;

                UIDocument? uiDoc =
                    uiApplication.ActiveUIDocument;

                if (uiDoc == null)
                {
                    message = "Open a Revit project first.";
                    return Result.Failed;
                }

                // ======================================
                // 1. INITIALISE WALL SELECTION EVENT
                // Existing functionality
                // ======================================

                PerformanceSelectionEventManager.Initialize();

                // ======================================
                // 2. INITIALISE PROJECT ANALYSIS EVENT
                // New functionality
                // ======================================

                ProjectAnalysisEventManager.Initialize();

                // ======================================
                // 3. SELECT PERFORMANCE DASHBOARD TAB
                // ======================================

                ComplianceModePaneProvider.SelectPerformanceTab();

                // ======================================
                // 4. OPEN SHARED DOCKABLE PANE
                // ======================================

                DockablePane pane =
                    uiApplication.GetDockablePane(
                        ComplianceModePaneProvider.PaneId);

                pane.Show();

                // ======================================
                // 5. UPDATE DASHBOARD STATUS
                // ======================================

                ComplianceModePaneProvider.UpdatePerformanceStatus(
                    "Performance dashboard ready. " +
                    "Wall selection and project analysis services initialized.");

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}
