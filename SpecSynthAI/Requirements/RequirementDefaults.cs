using SpecSynthAI.Requirements;

namespace SpecSynthAI.Requirements
{
    /// <summary>
    /// Compatibility wrapper for existing commands.
    /// Existing code that uses RequirementDefaults.ThermalConductivity now reads
    /// the saved project requirement instead of a hardcoded threshold.
    /// </summary>
    public static class RequirementDefaults
    {
        public static ProjectRequirement ThermalConductivity =>
            ProjectRequirementsStore.LoadThermalConductivity();
    }
}
