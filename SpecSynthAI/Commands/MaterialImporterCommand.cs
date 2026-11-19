using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using SpecSynthAI.UI;

namespace SpecSynthAI.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class MaterialImporterCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                var handler = new MaterialImportEventHandler();
                ExternalEvent externalEvent = ExternalEvent.Create(handler);
                var window = new MaterialImporterWindow();

                // The window event delegate retains the handler and ExternalEvent
                // while the modeless window remains open.
                window.ImportRequested += product =>
                {
                    try
                    {
                        handler.RequestImport(product);
                        ExternalEventRequest request = externalEvent.Raise();
                        if (request != ExternalEventRequest.Accepted &&
                            request != ExternalEventRequest.Pending)
                        {
                            MessageBox.Show(
                                $"Revit did not accept the import request: {request}",
                                "SpecSynth AI",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "SpecSynth AI - Import Request",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                new WindowInteropHelper(window).Owner =
                    commandData.Application.MainWindowHandle;

                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}
