using System;
using System.IO;
using System.Linq;
using System.Windows;

using Microsoft.Win32;

using ThermalEngine.UI.Services;
using ThermalEngine.UI.ViewModels;

namespace ThermalEngine.UI;

public partial class MainWindow : Window
{
    private readonly ProjectWallJsonImportService _importService;

    private readonly DashboardViewModel _viewModel;


    public MainWindow()
    {
        InitializeComponent();

        _importService = new ProjectWallJsonImportService();

        _viewModel = new DashboardViewModel();

        DataContext = _viewModel;

        LoadProjectPerformanceReport();
    }


    private void LoadProjectPerformanceReport()
    {
        try
        {
            string exportDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "SpecSynth_Exports"
            );


            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Revit Project Wall Export",

                Filter = "JSON Files (*.json)|*.json",

                InitialDirectory = Directory.Exists(exportDirectory)
                    ? exportDirectory
                    : Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),

                CheckFileExists = true,

                Multiselect = false
            };


            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult != true)
                return;


            var project = _importService.ImportProject(
                dialog.FileName
            );


            _viewModel.LoadProjectData(project);


            MessageBox.Show(
                $"Project performance data loaded successfully.\n\n" +

                $"Project: {project.ProjectName}\n\n" +

                $"Placed walls: {project.TotalPlacedWalls}\n" +

                $"Unique wall types: {project.UniqueWallTypes}\n\n" +

                $"Successful extractions: {project.SuccessfulExtractions}\n" +

                $"Failed extractions: {project.FailedExtractions}\n\n" +

                $"Valid calculations: {project.ValidAssemblies}\n" +

                $"Invalid calculations: {project.InvalidAssemblies}\n\n" +

                $"Best assembly:\n{project.Report.BestPerformingAssembly}\n\n" +

                $"Lowest U-value: {project.Report.LowestUValue:F4} W/(m²·K)",

                "SpecSynth - Import Successful",

                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to load project performance data.\n\n{ex.Message}",

                "SpecSynth - Import Error",

                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}