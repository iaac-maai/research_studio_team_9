namespace SpecSynthAI.UnifiedData.Models
{
    public sealed class UnifiedProductDataset
    {
        public string DatasetName { get; init; } = "Unknown";
        public string DatasetVersion { get; init; } = "Unknown";
        public string SourcePath { get; init; } = "";
        public List<UnifiedProductRecord> Products { get; init; } = new();
    }
}
