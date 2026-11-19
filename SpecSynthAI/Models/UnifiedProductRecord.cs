using System.Text.Json.Nodes;

namespace SpecSynthAI.UnifiedData.Models
{
    public enum UnifiedRecordType
    {
        StandaloneProduct,
        ProductFamily,
        ProductVariant,
        Unknown
    }

    public sealed class UnifiedProductRecord
    {
        public string ProductId { get; init; } = "";
        public string ProductName { get; init; } = "";
        public string Manufacturer { get; init; } = "";
        public UnifiedRecordType RecordType { get; init; } = UnifiedRecordType.Unknown;
        public string? FamilyId { get; init; }
        public string? ParentProductId { get; init; }
        public string VerificationStatus { get; init; } = "";
        public JsonObject RawJson { get; init; } = new();

        public JsonObject? Section(string name) =>
            RawJson[name] as JsonObject;

        public string ToFormattedJson() =>
            RawJson.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
    }
}
