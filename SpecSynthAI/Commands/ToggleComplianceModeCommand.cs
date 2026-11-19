using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpecSynthAI.Models;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ToggleComplianceModeCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Select the Compliance tab.
                ComplianceModePaneProvider.SelectComplianceTab();

                // Open the shared SpecSynth AI pane.
                DockablePane pane =
                    commandData.Application.GetDockablePane(
                        ComplianceModePaneProvider.PaneId);

                pane.Show();

                // Preserve existing heatmap functionality.
                if (ComplianceHeatmapSession.IsActive)
                {
                    var reset = new ResetComplianceHeatmapCommand();

                    return reset.Execute(
                        commandData,
                        ref message,
                        elements);
                }
                else
                {
                    var apply = new ApplyComplianceHeatmapCommand();

                    return apply.Execute(
                        commandData,
                        ref message,
                        elements);
                }
            }
            catch (System.Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "SpecSynth AI",
                    "Could not toggle Compliance Mode:\n\n" + ex.Message);

                return Result.Failed;
            }
        }
    }
}