using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SpecSynthAI.Models;

namespace SpecSynthAI.Requirements
{
    public static class ProjectRequirementsStore
    {
        private const double DefaultMaximumThermalConductivity = 0.022;
        private const double DefaultExternalWallUValue = 0.30;
        private const double DefaultInternalWallUValue = 0.60;
        private const double DefaultModerateTolerance = 10.0;

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
                JsonSerializer.Deserialize<ProjectRequirementsConfig>(
                    json, JsonOptions);

            if (config?.Requirements == null)
            {
                throw new InvalidDataException(
                    "Invalid project requirements configuration.");
            }

            return config;
        }

        public static ProjectRequirement LoadThermalConductivity()
        {
            return LoadRequirement(
                "REQ-THERMAL-001",
                "ThermalConductivity",
                DefaultMaximumThermalConductivity,
                "W/(m·K)",
                "Maximum permitted thermal conductivity for this project");
        }

        public static ProjectRequirement LoadExternalWallUValue()
        {
            return LoadRequirement(
                "REQ-THERMAL-002",
                "ExternalWallUValue",
                DefaultExternalWallUValue,
                "W/(m²·K)",
                "Maximum permitted external wall U-value");
        }

        public static ProjectRequirement LoadInternalWallUValue()
        {
            return LoadRequirement(
                "REQ-THERMAL-003",
                "InternalWallUValue",
                DefaultInternalWallUValue,
                "W/(m²·K)",
                "Maximum permitted internal wall U-value");
        }

        public static ProjectRequirement LoadModerateTolerance()
        {
            return LoadRequirement(
                "REQ-THERMAL-004",
                "ModeratePerformanceTolerance",
                DefaultModerateTolerance,
                "%",
                "Tolerance above the target for moderate performance classification");
        }

        private static ProjectRequirement LoadRequirement(
            string id,
            string propertyName,
            double defaultValue,
            string unit,
            string description)
        {
            ProjectRequirementsConfig config = Load();

            ProjectRequirement? requirement =
                config.Requirements.FirstOrDefault(item =>
                    string.Equals(
                        item.PropertyName?.Trim(),
                        propertyName,
                        StringComparison.OrdinalIgnoreCase));

            if (requirement == null)
            {
                requirement = new ProjectRequirement
                {
                    RequirementId = id,
                    PropertyName = propertyName,
                    MaximumAllowedValue = defaultValue,
                    Unit = unit,
                    Description = description
                };
            }

            ValidateValue(requirement.MaximumAllowedValue, propertyName);

            return requirement;
        }

        public static void SaveThermalConductivity(double value)
        {
            SaveRequirement(
                "REQ-THERMAL-001",
                "ThermalConductivity",
                value,
                "W/(m·K)",
                "Maximum permitted thermal conductivity for this project");
        }

        public static void SaveExternalWallUValue(double value)
        {
            SaveRequirement(
                "REQ-THERMAL-002",
                "ExternalWallUValue",
                value,
                "W/(m²·K)",
                "Maximum permitted external wall U-value");
        }

        public static void SaveInternalWallUValue(double value)
        {
            SaveRequirement(
                "REQ-THERMAL-003",
                "InternalWallUValue",
                value,
                "W/(m²·K)",
                "Maximum permitted internal wall U-value");
        }

        public static void SaveModerateTolerance(double value)
        {
            SaveRequirement(
                "REQ-THERMAL-004",
                "ModeratePerformanceTolerance",
                value,
                "%",
                "Tolerance above the target for moderate performance classification");
        }

        private static void SaveRequirement(
            string id,
            string propertyName,
            double value,
            string unit,
            string description)
        {
            ValidateValue(value, propertyName);

            if (propertyName == "ModeratePerformanceTolerance" &&
                value > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Tolerance cannot exceed 100%.");
            }

            ProjectRequirementsConfig config = Load();

            ProjectRequirement? requirement =
                config.Requirements.FirstOrDefault(item =>
                    string.Equals(
                        item.PropertyName?.Trim(),
                        propertyName,
                        StringComparison.OrdinalIgnoreCase));

            if (requirement == null)
            {
                requirement = new ProjectRequirement();
                config.Requirements.Add(requirement);
            }

            requirement.RequirementId = id;
            requirement.PropertyName = propertyName;
            requirement.MaximumAllowedValue = value;
            requirement.Unit = unit;
            requirement.Description = description;

            string? directory = Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(
                config, JsonOptions);

            // Preserve the previous configuration as a backup.
            if (File.Exists(FilePath))
            {
                string backupPath = FilePath + ".bak";
                File.Copy(FilePath, backupPath, true);
            }

            File.WriteAllText(FilePath, json);
        }

        private static void ValidateValue(
            double value,
            string propertyName)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0)
            {
                throw new InvalidDataException(
                    $"{propertyName} must be a positive finite number.");
            }
        }

        private static ProjectRequirementsConfig CreateDefaultConfiguration()
        {
            var config = new ProjectRequirementsConfig();

            config.Requirements.Add(
                CreateRequirement(
                    "REQ-THERMAL-001",
                    "ThermalConductivity",
                    DefaultMaximumThermalConductivity,
                    "W/(m·K)",
                    "Maximum permitted thermal conductivity for this project"));

            config.Requirements.Add(
                CreateRequirement(
                    "REQ-THERMAL-002",
                    "ExternalWallUValue",
                    DefaultExternalWallUValue,
                    "W/(m²·K)",
                    "Maximum permitted external wall U-value"));

            config.Requirements.Add(
                CreateRequirement(
                    "REQ-THERMAL-003",
                    "InternalWallUValue",
                    DefaultInternalWallUValue,
                    "W/(m²·K)",
                    "Maximum permitted internal wall U-value"));

            config.Requirements.Add(
                CreateRequirement(
                    "REQ-THERMAL-004",
                    "ModeratePerformanceTolerance",
                    DefaultModerateTolerance,
                    "%",
                    "Tolerance above the target for moderate performance classification"));

            return config;
        }

        private static ProjectRequirement CreateRequirement(
            string id,
            string propertyName,
            double value,
            string unit,
            string description)
        {
            return new ProjectRequirement
            {
                RequirementId = id,
                PropertyName = propertyName,
                MaximumAllowedValue = value,
                Unit = unit,
                Description = description
            };
        }
    }
}
