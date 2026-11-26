using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThermalEngine.Models;

namespace ThermalEngine.UI.ViewModels;

public class AssemblyDisplayItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string AssemblyName { get; set; } = "";

    public double UValue { get; set; }

    private double _improvementPercentage;

    public double ImprovementPercentage
    {
        get => _improvementPercentage;
        set
        {
            if (Math.Abs(_improvementPercentage - value) < 0.000001)
                return;

            _improvementPercentage = value;
            OnPropertyChanged();
        }
    }

    public int Rank { get; set; }

    public bool IsValid { get; set; }

    public string ErrorMessage { get; set; } = "";

    // Resistance breakdown.
    public double LayerResistance { get; set; }

    public double InternalSurfaceResistance { get; set; }

    public double ExternalSurfaceResistance { get; set; }

    public double TotalResistance { get; set; }

    // Individual material layers.
    public List<ThermalLayerResult> LayerResults { get; set; } = new();

    public string Status =>
        IsValid ? "VALID" : "INVALID";

    public string PerformanceColor
    {
        get
        {
            if (!IsValid)
                return "#8490A3";

            return Rank switch
            {
                1 => "#159B72",
                2 => "#4776E6",
                _ => "#D96B6B"
            };
        }
    }

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}