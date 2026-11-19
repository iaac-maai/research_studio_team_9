using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SpecSynthAI.Models;

namespace SpecSynthAI.Requirements
{
    /// <summary>
    /// Loads and saves project requirements in the current Windows user's Local AppData.
    /// This avoids writing configuration files into the protected Revit Program Files folder.
    /// </summary>
    public static class ProjectRequirementsStore
    {
        private const double DefaultMaximumThermalConductivity = 0.022;
        private const string ThermalRequirementId = "REQ-THERMAL-001";
        private const string ThermalPropertyName = "ThermalConductivity";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        public static string FilePath
        {
            get
            {
                string localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

                return Path.Combine(
                    localAppData,
                    "SpecSynthAI",
                    "project_requirements.json");
            }
        }

        public static ProjectRequirementsConfig Load()
        {
            if (!File.Exists(FilePath))
                return CreateDefaultConfiguration();

            string json = File.ReadAllText(FilePath);

            ProjectRequirementsConfig? config =
                JsonSerializer.Deserialize<ProjectRequirementsConfig>(json, JsonOptions);

            if (config?.Requirements == null)
            {
                throw new InvalidDataException(
                    $"The requirements file does not contain a valid 'requirements' list:\n{FilePath}");
            }

            return config;
        }

        public static ProjectRequirement LoadThermalConductivity()
        {
            ProjectRequirementsConfig config = Load();

            ProjectRequirement? requirement = config.Requirements.FirstOrDefault(item =>
                string.Equals(item.PropertyName?.Trim(), ThermalPropertyName,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.RequirementId?.Trim(), ThermalRequirementId,
                    StringComparison.OrdinalIgnoreCase));

            if (requirement == null)
                return CreateDefaultThermalRequirement();

            ValidateThermalRequirement(requirement);
            return requirement;
        }

        public static void SaveThermalConductivity(double maximumAllowedValue)
        {
            if (double.IsNaN(maximumAllowedValue) ||
                double.IsInfinity(maximumAllowedValue) ||
                maximumAllowedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumAllowedValue),
                    "Maximum thermal conductivity must be a positive finite number.");
            }

            ProjectRequirementsConfig config = Load();

            ProjectRequirement? existing = config.Requirements.FirstOrDefault(item =>
                string.Equals(item.PropertyName?.Trim(), ThermalPropertyName,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.RequirementId?.Trim(), ThermalRequirementId,
                    StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                existing = CreateDefaultThermalRequirement();
                config.Requirements.Add(existing);
            }

            existing.RequirementId = ThermalRequirementId;
            existing.PropertyName = ThermalPropertyName;
            existing.MaximumAllowedValue = maximumAllowedValue;
            existing.Unit = "W/(m·K)";
            existing.Description = "Maximum permitted thermal conductivity for this project";

            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(FilePath, json);
        }

        public static ProjectRequirement CreateDefaultThermalRequirement()
        {
            return new ProjectRequirement
            {
                RequirementId = ThermalRequirementId,
                PropertyName = ThermalPropertyName,
                MaximumAllowedValue = DefaultMaximumThermalConductivity,
                Unit = "W/(m·K)",
                Description = "Maximum permitted thermal conductivity for this project"
            };
        }

        private static ProjectRequirementsConfig CreateDefaultConfiguration()
        {
            return new ProjectRequirementsConfig
            {
                Requirements = { CreateDefaultThermalRequirement() }
            };
        }

        private static void ValidateThermalRequirement(ProjectRequirement requirement)
        {
            double value = requirement.MaximumAllowedValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new InvalidDataException(
                    "The maximum thermal conductivity in project_requirements.json must be a positive finite number.");
            }
        }
    }
}
