using System.Collections.Generic;
using SpecSynthAI.Models;

namespace SpecSynthAI.Requirements
{
    /// <summary>
    /// Stores the set of project requirements loaded from project_requirements.json.
    /// </summary>
    public class ProjectRequirementsConfig
    {
        public List<ProjectRequirement> Requirements { get; set; } = new();
    }
}
