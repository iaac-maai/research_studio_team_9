using System.Collections.Generic;

namespace SpecSynthAI.AssemblyOptimisation.Models;

public sealed class AssemblyGenerationReport
{
    public string DatasetVersion { get; set; } = "";
    public int ProductsRead { get; set; }
    public int CandidatesGenerated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<AssemblyCandidate> Candidates { get; set; } = new();
}
