using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class OpenComplianceModePaneCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                commandData.Application
                    .GetDockablePane(ComplianceModePaneProvider.PaneId)
                    .Show();

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show("SpecSynth AI", "Could not open Compliance Mode panel:\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}
