using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpecSynthAI.Requirements;
using SpecSynthAI.UI;
using System;
using System.Windows.Interop;

namespace SpecSynthAI.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class OpenRequirementsDialogCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                var dialog = new RequirementsDialog();
                new WindowInteropHelper(dialog).Owner =
                    commandData.Application.MainWindowHandle;

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    TaskDialog.Show(
                        "SpecSynth AI",
                        "Project requirements saved successfully.\n\n" +
                        $"Configuration file:\n{ProjectRequirementsStore.FilePath}\n\n" +
                        "Run the existing Inspect Element command to check a wall using the updated requirement.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show(
                    "SpecSynth AI - Requirements",
                    "Could not open or save project requirements.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}
