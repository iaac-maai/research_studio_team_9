using SpecSynthAI.Models;

namespace SpecSynthAI.Requirements
{
    public class ProjectRequirement
    {
        public string RequirementId { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public double MaximumAllowedValue { get; set; }
        public string Unit { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
