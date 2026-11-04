using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace SpecSynthAI
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string tabName = "SpecSynth AI";

            // Reuse the existing SpecSynth AI tab if it has already been created.
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // The tab already exists. Continue adding our panel/buttons.
            }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Inspection");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string iconDirectory = Path.GetDirectoryName(assemblyPath) ?? "";

            string inspectIconPath = Path.Combine(iconDirectory, "InspectElement.png");
            string exportIconPath = Path.Combine(iconDirectory, "ExportThermalSchedule.png");

            PushButtonData inspectButtonData = new PushButtonData(
                "InspectElement",
                "Inspect\nElement",
                assemblyPath,
                "SpecSynthAI.Commands.InspectElementCommand");

            if (File.Exists(inspectIconPath))
                inspectButtonData.LargeImage = LoadRibbonIcon(inspectIconPath);

            panel.AddItem(inspectButtonData);

            PushButtonData exportButtonData = new PushButtonData(
                "ExportThermalSchedule",
                "Export Thermal\nSchedule",
                assemblyPath,
                "SpecSynthAI.Commands.ExportThermalScheduleCommand");

            if (File.Exists(exportIconPath))
                exportButtonData.LargeImage = LoadRibbonIcon(exportIconPath);

            panel.AddItem(exportButtonData);

            // New button: opens the requirements dialog in the same SpecSynth AI ribbon panel.
            PushButtonData requirementsButtonData = new PushButtonData(
                "ProjectRequirements",
                "Project\nRequirements",
                assemblyPath,
                "SpecSynthAI.Commands.OpenRequirementsDialogCommand");

            string requirementsIconPath = Path.Combine(iconDirectory, "ProjectRequirements.png");
            if (File.Exists(requirementsIconPath))
                requirementsButtonData.LargeImage = LoadRibbonIcon(requirementsIconPath);

            panel.AddItem(requirementsButtonData);

            return Result.Succeeded;
        }

        private static BitmapImage LoadRibbonIcon(string imagePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(imagePath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
