using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpecSynthAI.Models
{
    public class Product
    {
        public string ProductId { get; set; } = "";

        public string Manufacturer { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string Category { get; set; } = "";

        public double ThicknessMm { get; set; }

        [JsonPropertyName("thermal_conductivity_W_mK")]
        public double? ThermalConductivityWmK { get; set; }

        public double CompressiveStrengthKPa { get; set; }

        public double CompressiveStrengthToleranceKPa { get; set; }

        public string ReactionToFire { get; set; } = "";

        public string SourceUrl { get; set; } = "";

        public string SourceDocument { get; set; } = "";

        public string VerificationStatus { get; set; } = "";

        public string RevitMaterialName { get; set; } = "";

        public string MaterialFamily { get; set; } = "";

        public List<double> AvailableThicknessesMm { get; set; } = new();
    }
}