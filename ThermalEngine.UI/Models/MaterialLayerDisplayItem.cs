namespace ThermalEngine.UI.Models;

public class MaterialLayerDisplayItem
{
    public string LayerName { get; set; } = "";

    public double ThicknessMm { get; set; }

    public double ThermalConductivityWmK { get; set; }

    public double ThermalResistance { get; set; }

    public double ResistanceContributionPercentage { get; set; }

    public int ConductivitySource { get; set; }

    public string ConductivitySourceName =>
        ConductivitySource switch
        {
            1 => "Manufacturer verified",

            2 => "Revit thermal asset",

            _ => "Unknown / unverified"
        };

    public string ConductivitySourceColor =>
        ConductivitySource switch
        {
            1 => "#159B72",

            2 => "#4776E6",

            _ => "#D99B35"
        };
}