using System.Collections.Generic;

namespace SpecSynthAI.UnifiedData.Models
{
    public class ProductAssessment
    {
        public string ProductId { get; set; } = "";

        public UnifiedRecordType RecordType { get; set; }

        public bool IdentityComplete { get; set; }

        public bool TechnicalComplete { get; set; }

        public bool IsRevitImportEligible { get; set; }

        public List<string> IdentityIssues { get; set; } = new();

        public List<string> TechnicalIssues { get; set; } = new();

        public List<string> ImportBlockers { get; set; } = new();

        public List<string> TechnicalWarnings { get; set; } = new();
    }
}