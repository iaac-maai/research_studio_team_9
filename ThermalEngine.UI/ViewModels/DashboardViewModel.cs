using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using ThermalEngine.Models;
using ThermalEngine.UI.Models;

namespace ThermalEngine.UI.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private AssemblyDisplayItem? _selectedAssembly;

    private AssemblyDisplayItem? _selectedBaselineAssembly;


    // ============================================
    // PROJECT INFORMATION
    // ============================================

    public string ProjectName { get; set; } = "";

    public DateTime ExportDate { get; set; }

    public int TotalPlacedWalls { get; set; }

    public int UniqueWallTypes { get; set; }

    public int SuccessfulExtractions { get; set; }

    public int FailedExtractions { get; set; }

    public int ValidAssemblyCount { get; set; }

    public int InvalidAssemblyCount { get; set; }

    public int UnsupportedAssemblyCount { get; set; }


    // ============================================
    // THERMAL PERFORMANCE SUMMARY
    // ============================================

    public int AssemblyCount { get; set; }

    public double BestUValue { get; set; }

    public string BestAssemblyName { get; set; } = "";

    public double BestImprovement { get; set; }

    public string BaselineAssemblyName { get; set; } = "";

    public double BaselineUValue { get; set; }


    // ============================================
    // ASSEMBLIES
    // ============================================

    public ObservableCollection<AssemblyDisplayItem> Assemblies
    {
        get;
        set;
    } = new();


    public ObservableCollection<AssemblyDisplayItem> BaselineOptions
    {
        get;
        set;
    } = new();


    // ============================================
    // SELECTED BASELINE
    // ============================================

    public AssemblyDisplayItem? SelectedBaselineAssembly
    {
        get => _selectedBaselineAssembly;

        set
        {
            if (_selectedBaselineAssembly == value)
                return;

            if (value != null && !value.IsValid)
                return;

            _selectedBaselineAssembly = value;

            OnPropertyChanged();

            RecalculateBaselineComparison();
        }
    }


    // ============================================
    // SELECTED ASSEMBLY
    // ============================================

    public AssemblyDisplayItem? SelectedAssembly
    {
        get => _selectedAssembly;

        set
        {
            if (_selectedAssembly == value)
                return;

            _selectedAssembly = value;

            OnPropertyChanged();

            LoadSelectedAssembly();
        }
    }


    // ============================================
    // SELECTED ASSEMBLY STATUS
    // ============================================

    public string SelectedAssemblyStatus { get; set; } = "";

    public string SelectedAssemblyStatusMessage { get; set; } = "";

    public string SelectedAssemblyStatusColor { get; set; } = "#8490A3";

    public bool ShowThermalResults { get; set; }

    public bool ShowStatusPanel { get; set; }


    // ============================================
    // SELECTED ASSEMBLY PERFORMANCE
    // ============================================

    public double SelectedAssemblyUValue { get; set; }

    public double SelectedAssemblyImprovement { get; set; }

    public double SelectedAssemblyTotalResistance { get; set; }

    public int SelectedAssemblyRank { get; set; }

    public string SelectedAssemblyName { get; set; } = "";


    // ============================================
    // RESISTANCE BREAKDOWN
    // ============================================

    public double SelectedAssemblyLayerResistance { get; set; }

    public double SelectedAssemblyInternalSurfaceResistance { get; set; }

    public double SelectedAssemblyExternalSurfaceResistance { get; set; }


    // ============================================
    // MATERIAL LAYERS
    // ============================================

    public ObservableCollection<MaterialLayerDisplayItem>
        BestAssemblyLayers
    { get; set; } = new();

    public double BestAssemblyTotalResistance { get; set; }


    // ============================================
    // PROJECT WALL RECORDS
    // ============================================

    public ObservableCollection<ProjectWallRecord> WallRecords
    {
        get;
        set;
    } = new();

    // ============================================
    // THERMAL CLASSIFICATION COUNTS
    // ============================================

    public int GoodWallCount =>
        GetClassificationCount("GOOD");

    public int ModerateWallCount =>
        GetClassificationCount("MODERATE");

    public int PoorWallCount =>
        GetClassificationCount("POOR");

    public int NotAnalysedWallCount =>
        GetClassificationCount("NOT ANALYSED");

    public int UnsupportedWallCount =>
        GetClassificationCount("UNSUPPORTED");

    public int ClassificationTotalCount =>
        GoodWallCount +
        ModerateWallCount +
        PoorWallCount +
        NotAnalysedWallCount +
        UnsupportedWallCount;


    // ============================================
    // CLASSIFICATION PERCENTAGES
    // ============================================

    public double GoodWallPercentage =>
        GetClassificationPercentage(GoodWallCount);

    public double ModerateWallPercentage =>
        GetClassificationPercentage(ModerateWallCount);

    public double PoorWallPercentage =>
        GetClassificationPercentage(PoorWallCount);

    public double NotAnalysedWallPercentage =>
        GetClassificationPercentage(NotAnalysedWallCount);

    public double UnsupportedWallPercentage =>
        GetClassificationPercentage(UnsupportedWallCount);


    // ============================================
    // CLASSIFICATION CALCULATION HELPERS
    // ============================================

    private int GetClassificationCount(string status)
    {
        return WallRecords
            .Where(wall =>
                string.Equals(
                    wall.ThermalClassificationStatus?.Trim(),
                    status,
                    StringComparison.OrdinalIgnoreCase))
            .Sum(wall => Math.Max(0, wall.InstanceCount));
    }

    private double GetClassificationPercentage(int count)
    {
        return ClassificationTotalCount == 0
            ? 0
            : (double)count / ClassificationTotalCount * 100;
    }
    // ============================================
    // LOAD COMPLETE PROJECT DATA
    // ============================================

    public void LoadProjectData(ProjectWallImportResult project)
    {
        ArgumentNullException.ThrowIfNull(project);

        ProjectName = project.ProjectName;

        ExportDate = project.ExportDate;

        TotalPlacedWalls = project.TotalPlacedWalls;

        UniqueWallTypes = project.UniqueWallTypes;

        SuccessfulExtractions = project.SuccessfulExtractions;

        FailedExtractions = project.FailedExtractions;

        ValidAssemblyCount = project.ValidAssemblies;

        InvalidAssemblyCount = project.InvalidAssemblies;

        UnsupportedAssemblyCount = project.UnsupportedAssemblies;


        WallRecords.Clear();

        foreach (var wall in project.WallTypes)
        {
            WallRecords.Add(wall);
        }


        LoadReport(project.Report);


        OnPropertyChanged(nameof(ProjectName));
        OnPropertyChanged(nameof(ExportDate));

        OnPropertyChanged(nameof(TotalPlacedWalls));
        OnPropertyChanged(nameof(UniqueWallTypes));

        OnPropertyChanged(nameof(SuccessfulExtractions));
        OnPropertyChanged(nameof(FailedExtractions));

        OnPropertyChanged(nameof(ValidAssemblyCount));
        OnPropertyChanged(nameof(InvalidAssemblyCount));
        OnPropertyChanged(nameof(UnsupportedAssemblyCount));

        OnPropertyChanged(nameof(WallRecords));

        // Classification count notifications
        OnPropertyChanged(nameof(GoodWallCount));
        OnPropertyChanged(nameof(ModerateWallCount));
        OnPropertyChanged(nameof(PoorWallCount));
        OnPropertyChanged(nameof(NotAnalysedWallCount));
        OnPropertyChanged(nameof(UnsupportedWallCount));

        OnPropertyChanged(nameof(ClassificationTotalCount));

        // Classification percentage notifications
        OnPropertyChanged(nameof(GoodWallPercentage));
        OnPropertyChanged(nameof(ModerateWallPercentage));
        OnPropertyChanged(nameof(PoorWallPercentage));
        OnPropertyChanged(nameof(NotAnalysedWallPercentage));
        OnPropertyChanged(nameof(UnsupportedWallPercentage));
    }


    // ============================================
    // LOAD THERMAL COMPARISON REPORT
    // ============================================

    public void LoadReport(WallComparisonReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Assemblies.Clear();

        BaselineOptions.Clear();


        AssemblyCount = report.Results.Count(
            result => result.IsValid);

        BestUValue = report.LowestUValue ?? 0;

        BestAssemblyName =
            report.BestPerformingAssembly ?? "Not available";


        int rank = 1;


        foreach (var result in report.Results)
        {
            var assembly = new AssemblyDisplayItem
            {
                AssemblyName = result.AssemblyName,

                UValue = result.UValue ?? 0,

                ImprovementPercentage =
                    result.ImprovementPercentage ?? 0,

                Rank = result.IsValid ? rank++ : 0,

                IsValid = result.IsValid,

                LayerResistance =
                    result.LayerResistance ?? 0,

                InternalSurfaceResistance =
                    result.InternalSurfaceResistance ?? 0,

                ExternalSurfaceResistance =
                    result.ExternalSurfaceResistance ?? 0,

                TotalResistance =
                    result.TotalResistance ?? 0,

                ErrorMessage = result.IsValid
                    ? ""
                    : string.Join("; ", result.Errors),

                LayerResults = result.LayerResults.ToList()
            };


            Assemblies.Add(assembly);


            if (assembly.IsValid)
            {
                BaselineOptions.Add(assembly);
            }
        }


        // Original baseline, if available.

        SelectedBaselineAssembly = BaselineOptions.FirstOrDefault(
            assembly =>
                assembly.AssemblyName == report.BaselineAssembly
        ) ?? BaselineOptions.FirstOrDefault();


        // Select best-performing assembly.

        SelectedAssembly = Assemblies.FirstOrDefault(
            assembly =>
                assembly.IsValid &&
                assembly.AssemblyName == BestAssemblyName
        ) ?? Assemblies.FirstOrDefault(
            assembly => assembly.IsValid
        );


        OnPropertyChanged(nameof(AssemblyCount));
        OnPropertyChanged(nameof(BestUValue));
        OnPropertyChanged(nameof(BestAssemblyName));

        OnPropertyChanged(nameof(Assemblies));
        OnPropertyChanged(nameof(BaselineOptions));
    }


    // ============================================
    // FIND ORIGINAL WALL RECORD
    // ============================================

    private ProjectWallRecord? FindWallRecord(string assemblyName)
    {
        return WallRecords.FirstOrDefault(
            wall =>
                wall.ThermalPerformance?.AssemblyName == assemblyName
                ||
                wall.WallTypeName == assemblyName);
    }


    // ============================================
    // DETERMINE ASSEMBLY STATUS
    // ============================================

    private void UpdateSelectedAssemblyStatus()
    {
        var wallRecord = FindWallRecord(
            SelectedAssembly!.AssemblyName);


        if (SelectedAssembly.IsValid)
        {
            SelectedAssemblyStatus = "VALID";

            SelectedAssemblyStatusMessage =
                "Thermal performance successfully calculated. " +
                "Material properties and conductivity sources are available.";

            SelectedAssemblyStatusColor = "#159B72";

            ShowThermalResults = true;

            ShowStatusPanel = false;

            return;
        }


        ShowThermalResults = false;

        ShowStatusPanel = true;


        if (wallRecord?.ThermalPerformance == null)
        {
            if (!string.IsNullOrWhiteSpace(wallRecord?.ExtractionError))
            {
                SelectedAssemblyStatus = "EXTRACTION FAILED";

                SelectedAssemblyStatusMessage =
                    wallRecord.ExtractionError;

                SelectedAssemblyStatusColor = "#D9534F";
            }
            else
            {
                SelectedAssemblyStatus = "UNSUPPORTED";

                SelectedAssemblyStatusMessage =
                    "Thermal performance data is unavailable for this wall. " +
                    "The wall may contain unsupported or incomplete material information.";

                SelectedAssemblyStatusColor = "#D99B35";
            }

            return;
        }


        SelectedAssemblyStatus = "INVALID CALCULATION";

        SelectedAssemblyStatusMessage =
            !string.IsNullOrWhiteSpace(SelectedAssembly.ErrorMessage)
                ? SelectedAssembly.ErrorMessage
                : "The wall was extracted, but its thermal calculation could not be completed.";

        SelectedAssemblyStatusColor = "#D99B35";
    }


    // ============================================
    // RECALCULATE BASELINE COMPARISON
    // ============================================

    private void RecalculateBaselineComparison()
    {
        if (SelectedBaselineAssembly == null)
            return;


        double baselineU = SelectedBaselineAssembly.UValue;

        if (baselineU <= 0)
            return;


        BaselineAssemblyName =
            SelectedBaselineAssembly.AssemblyName;

        BaselineUValue = baselineU;


        foreach (var assembly in Assemblies)
        {
            if (!assembly.IsValid)
                continue;


            double improvement =
                ((baselineU - assembly.UValue) / baselineU) * 100;


            assembly.ImprovementPercentage = improvement;
        }


        var bestAssembly = Assemblies
            .Where(assembly => assembly.IsValid)
            .OrderBy(assembly => assembly.UValue)
            .FirstOrDefault();


        BestImprovement =
            bestAssembly?.ImprovementPercentage ?? 0;


        LoadSelectedAssembly();


        OnPropertyChanged(nameof(BaselineAssemblyName));
        OnPropertyChanged(nameof(BaselineUValue));
        OnPropertyChanged(nameof(BestImprovement));
    }


    // ============================================
    // LOAD SELECTED ASSEMBLY
    // ============================================

    private void LoadSelectedAssembly()
    {
        BestAssemblyLayers.Clear();


        if (SelectedAssembly == null)
        {
            SelectedAssemblyName = "No assembly selected";

            SelectedAssemblyUValue = 0;

            SelectedAssemblyImprovement = 0;

            SelectedAssemblyRank = 0;

            SelectedAssemblyLayerResistance = 0;

            SelectedAssemblyInternalSurfaceResistance = 0;

            SelectedAssemblyExternalSurfaceResistance = 0;

            SelectedAssemblyTotalResistance = 0;

            BestAssemblyTotalResistance = 0;

            SelectedAssemblyStatus = "NO ASSEMBLY";

            SelectedAssemblyStatusMessage =
                "Select a wall assembly to inspect its thermal performance.";

            SelectedAssemblyStatusColor = "#8490A3";

            ShowThermalResults = false;

            ShowStatusPanel = true;

            NotifySelectedAssemblyProperties();

            return;
        }


        // Assembly information.

        SelectedAssemblyName =
            SelectedAssembly.AssemblyName;

        SelectedAssemblyUValue =
            SelectedAssembly.UValue;

        SelectedAssemblyImprovement =
            SelectedAssembly.ImprovementPercentage;

        SelectedAssemblyRank =
            SelectedAssembly.Rank;


        // Determine status before displaying results.

        UpdateSelectedAssemblyStatus();


        // Stop here if calculation is invalid.

        if (!SelectedAssembly.IsValid)
        {
            SelectedAssemblyLayerResistance = 0;

            SelectedAssemblyInternalSurfaceResistance = 0;

            SelectedAssemblyExternalSurfaceResistance = 0;

            SelectedAssemblyTotalResistance = 0;

            BestAssemblyTotalResistance = 0;

            NotifySelectedAssemblyProperties();

            return;
        }


        // Resistance breakdown.

        SelectedAssemblyLayerResistance =
            SelectedAssembly.LayerResistance;

        SelectedAssemblyInternalSurfaceResistance =
            SelectedAssembly.InternalSurfaceResistance;

        SelectedAssemblyExternalSurfaceResistance =
            SelectedAssembly.ExternalSurfaceResistance;

        SelectedAssemblyTotalResistance =
            SelectedAssembly.TotalResistance;

        BestAssemblyTotalResistance =
            SelectedAssembly.LayerResistance;


        // Find original project material data.

        var wallRecord = FindWallRecord(
            SelectedAssembly.AssemblyName);


        // Match calculated layers with original source information.

        foreach (var calculatedLayer in SelectedAssembly.LayerResults)
        {
            var sourceLayer = wallRecord?.Layers.FirstOrDefault(
                layer =>
                    layer.LayerName == calculatedLayer.LayerName);


            BestAssemblyLayers.Add(
                new MaterialLayerDisplayItem
                {
                    LayerName = calculatedLayer.LayerName,

                    ThicknessMm = calculatedLayer.ThicknessMm,

                    // ThermalLayerResult stores conductivity as a non-nullable double.
                    ThermalConductivityWmK =
                        calculatedLayer.ThermalConductivityWmK,

                    ThermalResistance =
                        calculatedLayer.ThermalResistance,

                    ResistanceContributionPercentage =
                        calculatedLayer.ResistanceContributionPercentage,

                    ConductivitySource =
                        sourceLayer?.ConductivitySource ?? 0
                });
        }


        NotifySelectedAssemblyProperties();
    }


    // ============================================
    // PROPERTY NOTIFICATIONS
    // ============================================

    private void NotifySelectedAssemblyProperties()
    {
        OnPropertyChanged(nameof(SelectedAssemblyName));

        OnPropertyChanged(nameof(SelectedAssemblyUValue));

        OnPropertyChanged(nameof(SelectedAssemblyImprovement));

        OnPropertyChanged(nameof(SelectedAssemblyRank));

        OnPropertyChanged(nameof(SelectedAssemblyLayerResistance));

        OnPropertyChanged(nameof(SelectedAssemblyInternalSurfaceResistance));

        OnPropertyChanged(nameof(SelectedAssemblyExternalSurfaceResistance));

        OnPropertyChanged(nameof(SelectedAssemblyTotalResistance));

        OnPropertyChanged(nameof(BestAssemblyTotalResistance));

        OnPropertyChanged(nameof(BestAssemblyLayers));

        OnPropertyChanged(nameof(SelectedAssemblyStatus));

        OnPropertyChanged(nameof(SelectedAssemblyStatusMessage));

        OnPropertyChanged(nameof(SelectedAssemblyStatusColor));

        OnPropertyChanged(nameof(ShowThermalResults));

        OnPropertyChanged(nameof(ShowStatusPanel));
    }


    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}